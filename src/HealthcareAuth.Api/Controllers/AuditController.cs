using HealthcareAuth.Api.Contracts;
using HealthcareAuth.Api.Data;
using HealthcareAuth.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAuth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class AuditController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AuditController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AuditLogResponse>>> GetAuditLogs(CancellationToken cancellationToken)
    {
        var logs = await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new AuditLogResponse(
                x.Id,
                x.UserName,
                x.Action,
                x.EntityName,
                x.EntityId,
                x.Details,
                x.IpAddress,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(logs);
    }
}
