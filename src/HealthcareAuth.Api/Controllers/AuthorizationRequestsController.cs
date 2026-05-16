using System.Security.Claims;
using Hangfire;
using HealthcareAuth.Api.Contracts;
using HealthcareAuth.Api.Data;
using HealthcareAuth.Api.Models;
using HealthcareAuth.Api.Options;
using HealthcareAuth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthcareAuth.Api.Controllers;

[ApiController]
[Route("api/authorizations")]
[Authorize(Policy = "ClinicalStaff")]
public class AuthorizationRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthorizationWorkflowService _workflowService;
    private readonly IAuditService _auditService;
    private readonly StorageOptions _storageOptions;
    private readonly IWebHostEnvironment _environment;

    public AuthorizationRequestsController(
        ApplicationDbContext db,
        IAuthorizationWorkflowService workflowService,
        IAuditService auditService,
        IOptions<StorageOptions> storageOptions,
        IWebHostEnvironment environment)
    {
        _db = db;
        _workflowService = workflowService;
        _auditService = auditService;
        _storageOptions = storageOptions.Value;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AuthorizationListItemResponse>>> GetRequests(
        [FromQuery] AuthorizationStatus? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = _db.AuthorizationRequests
            .Include(x => x.Patient)
            .AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.RequestNumber.Contains(term) ||
                x.RequestedService.Contains(term) ||
                x.Patient!.FirstName.Contains(term) ||
                x.Patient!.LastName.Contains(term) ||
                x.Patient!.MedicalRecordNumber.Contains(term));
        }

        var requests = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(150)
            .Select(x => x.ToListItem())
            .ToListAsync(cancellationToken);

        return Ok(requests);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuthorizationResponse>> GetRequest(int id, CancellationToken cancellationToken)
    {
        var request = await GetRequestQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return request is null ? NotFound() : Ok(request.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<AuthorizationResponse>> CreateRequest(AuthorizationCreateRequest request, CancellationToken cancellationToken)
    {
        var patientExists = await _db.Patients.AnyAsync(x => x.Id == request.PatientId, cancellationToken);
        if (!patientExists)
        {
            return BadRequest(new { message = "Patient does not exist." });
        }

        var entity = new AuthorizationRequest
        {
            RequestNumber = DbInitializer.CreateRequestNumber(),
            PatientId = request.PatientId,
            RequestedService = request.RequestedService,
            DiagnosisCode = request.DiagnosisCode,
            ProcedureCode = request.ProcedureCode,
            Priority = request.Priority,
            ClinicalNotes = request.ClinicalNotes,
            DueDate = request.DueDate,
            CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        _db.AuthorizationRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("Create", nameof(AuthorizationRequest), entity.Id.ToString(), $"Created {entity.RequestNumber}.", cancellationToken);

        var created = await GetRequestQuery().AsNoTracking().FirstAsync(x => x.Id == entity.Id, cancellationToken);
        return CreatedAtAction(nameof(GetRequest), new { id = entity.Id }, created.ToResponse());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AuthorizationResponse>> UpdateRequest(int id, AuthorizationUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.AuthorizationRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (entity.Status is AuthorizationStatus.Approved or AuthorizationStatus.Denied or AuthorizationStatus.Cancelled)
        {
            return Conflict(new { message = "Finalized requests cannot be edited." });
        }

        entity.RequestedService = request.RequestedService;
        entity.DiagnosisCode = request.DiagnosisCode;
        entity.ProcedureCode = request.ProcedureCode;
        entity.Priority = request.Priority;
        entity.ClinicalNotes = request.ClinicalNotes;
        entity.DueDate = request.DueDate;
        entity.LastUpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("Update", nameof(AuthorizationRequest), entity.Id.ToString(), $"Updated {entity.RequestNumber}.", cancellationToken);

        var updated = await GetRequestQuery().AsNoTracking().FirstAsync(x => x.Id == id, cancellationToken);
        return Ok(updated.ToResponse());
    }

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id, CancellationToken cancellationToken)
    {
        await _workflowService.SubmitAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
        return Accepted();
    }

    [HttpPost("{id:int}/reanalyze")]
    public IActionResult Reanalyze(int id)
    {
        BackgroundJob.Enqueue<IBackgroundProcessingService>(service => service.AnalyzeAuthorizationAsync(id));
        return Accepted();
    }

    [HttpPost("{id:int}/documents")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<MedicalDocumentResponse>> UploadDocument(int id, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { message = "File is empty." });
        }

        var authorizationExists = await _db.AuthorizationRequests.AnyAsync(x => x.Id == id, cancellationToken);
        if (!authorizationExists)
        {
            return NotFound();
        }

        var uploadRoot = Path.Combine(_environment.ContentRootPath, _storageOptions.UploadRoot);
        Directory.CreateDirectory(uploadRoot);

        var safeExtension = Path.GetExtension(file.FileName);
        var storedFileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(uploadRoot, storedFileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var document = new MedicalDocument
        {
            AuthorizationRequestId = id,
            FileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            ContentType = file.ContentType,
            FilePath = filePath,
            FileSize = file.Length,
            UploadedById = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        _db.MedicalDocuments.Add(document);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("UploadDocument", nameof(MedicalDocument), document.Id.ToString(), $"Uploaded {document.FileName}.", cancellationToken);

        BackgroundJob.Enqueue<IBackgroundProcessingService>(service => service.ProcessDocumentAsync(document.Id));

        return Accepted(new MedicalDocumentResponse(
            document.Id,
            document.FileName,
            document.ContentType,
            document.FileSize,
            document.OcrStatus,
            document.OcrText,
            document.OcrError,
            document.UploadedAt));
    }

    [HttpPost("{id:int}/url-attachments")]
    public async Task<ActionResult<UrlAttachmentResponse>> AddUrlAttachment(int id, UrlAttachmentCreateRequest request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        {
            return BadRequest(new { message = "URL must be absolute." });
        }

        var authorizationExists = await _db.AuthorizationRequests.AnyAsync(x => x.Id == id, cancellationToken);
        if (!authorizationExists)
        {
            return NotFound();
        }

        var attachment = new UrlAttachment
        {
            AuthorizationRequestId = id,
            Title = request.Title,
            Url = request.Url,
            Description = request.Description
        };

        _db.UrlAttachments.Add(attachment);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("AttachUrl", nameof(UrlAttachment), attachment.Id.ToString(), $"Attached {request.Url}.", cancellationToken);

        return CreatedAtAction(nameof(GetRequest), new { id }, new UrlAttachmentResponse(attachment.Id, attachment.Title, attachment.Url, attachment.Description, attachment.CreatedAt));
    }

    private IQueryable<AuthorizationRequest> GetRequestQuery()
    {
        return _db.AuthorizationRequests
            .Include(x => x.Patient)
            .Include(x => x.Documents)
            .Include(x => x.UrlAttachments)
            .Include(x => x.StatusHistory);
    }
}
