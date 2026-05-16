using HealthcareAuth.Api.Contracts;
using HealthcareAuth.Api.Data;
using HealthcareAuth.Api.Models;
using HealthcareAuth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAuth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ClinicalStaff")]
public class PatientsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _auditService;

    public PatientsController(ApplicationDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PatientResponse>>> GetPatients([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = _db.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.FirstName.Contains(term) ||
                x.LastName.Contains(term) ||
                x.MedicalRecordNumber.Contains(term) ||
                x.MemberNumber.Contains(term));
        }

        var patients = await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Take(100)
            .Select(x => x.ToResponse())
            .ToListAsync(cancellationToken);

        return Ok(patients);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PatientResponse>> GetPatient(int id, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return patient is null ? NotFound() : Ok(patient.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<PatientResponse>> CreatePatient(PatientCreateRequest request, CancellationToken cancellationToken)
    {
        var exists = await _db.Patients.AnyAsync(x => x.MedicalRecordNumber == request.MedicalRecordNumber, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "A patient with that medical record number already exists." });
        }

        var patient = new Patient
        {
            MedicalRecordNumber = request.MedicalRecordNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Phone = request.Phone,
            Email = request.Email,
            InsuranceProvider = request.InsuranceProvider,
            MemberNumber = request.MemberNumber
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("Create", nameof(Patient), patient.Id.ToString(), $"Created patient {patient.MedicalRecordNumber}.", cancellationToken);

        return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, patient.ToResponse());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PatientResponse>> UpdatePatient(int id, PatientUpdateRequest request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (patient is null)
        {
            return NotFound();
        }

        patient.FirstName = request.FirstName;
        patient.LastName = request.LastName;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender;
        patient.Phone = request.Phone;
        patient.Email = request.Email;
        patient.InsuranceProvider = request.InsuranceProvider;
        patient.MemberNumber = request.MemberNumber;
        patient.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("Update", nameof(Patient), patient.Id.ToString(), $"Updated patient {patient.MedicalRecordNumber}.", cancellationToken);

        return Ok(patient.ToResponse());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeletePatient(int id, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (patient is null)
        {
            return NotFound();
        }

        _db.Patients.Remove(patient);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("Delete", nameof(Patient), patient.Id.ToString(), $"Deleted patient {patient.MedicalRecordNumber}.", cancellationToken);

        return NoContent();
    }
}
