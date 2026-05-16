using HealthcareAuth.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAuth.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize(Policy = "ClinicalStaff")]
public class DocumentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DocumentsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var document = await _db.MedicalDocuments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (document is null || !System.IO.File.Exists(document.FilePath))
        {
            return NotFound();
        }

        var stream = System.IO.File.OpenRead(document.FilePath);
        return File(stream, document.ContentType, document.FileName);
    }
}
