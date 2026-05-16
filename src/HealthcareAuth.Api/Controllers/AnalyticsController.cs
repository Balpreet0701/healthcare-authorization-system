using HealthcareAuth.Api.Contracts;
using HealthcareAuth.Api.Data;
using HealthcareAuth.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAuth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ReviewerOnly")]
public class AnalyticsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AnalyticsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<AnalyticsResponse>> GetAnalytics(CancellationToken cancellationToken)
    {
        var requests = await _db.AuthorizationRequests.AsNoTracking().ToListAsync(cancellationToken);
        var statusCounts = requests
            .GroupBy(x => x.Status.ToString())
            .Select(x => new StatusCountResponse(x.Key, x.Count()))
            .OrderBy(x => x.Status)
            .ToList();

        var priorityCounts = requests
            .GroupBy(x => x.Priority.ToString())
            .Select(x => new StatusCountResponse(x.Key, x.Count()))
            .OrderBy(x => x.Status)
            .ToList();

        var completed = requests
            .Where(x => x.SubmittedAt.HasValue && x.Status is AuthorizationStatus.Approved or AuthorizationStatus.Denied)
            .Select(x => (x.LastUpdatedAt - x.SubmittedAt!.Value).TotalHours)
            .ToList();

        var response = new AnalyticsResponse(
            requests.Count,
            requests.Count(x => x.Status is AuthorizationStatus.Submitted or AuthorizationStatus.InReview or AuthorizationStatus.PendingInformation),
            requests.Count(x => x.Status == AuthorizationStatus.Approved),
            requests.Count(x => x.Status == AuthorizationStatus.Denied),
            await _db.MedicalDocuments.CountAsync(x => x.OcrStatus == OcrStatus.Completed, cancellationToken),
            completed.Count == 0 ? 0 : Math.Round(completed.Average(), 2),
            statusCounts,
            priorityCounts);

        return Ok(response);
    }
}
