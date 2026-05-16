using System.Security.Claims;
using HealthcareAuth.Api.Contracts;
using HealthcareAuth.Api.Data;
using HealthcareAuth.Api.Models;
using HealthcareAuth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAuth.Api.Controllers;

[ApiController]
[Route("api/reviewer")]
[Authorize(Policy = "ReviewerOnly")]
public class ReviewerController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthorizationWorkflowService _workflowService;

    public ReviewerController(ApplicationDbContext db, IAuthorizationWorkflowService workflowService)
    {
        _db = db;
        _workflowService = workflowService;
    }

    [HttpGet("queue")]
    public async Task<ActionResult<IReadOnlyCollection<AuthorizationListItemResponse>>> GetQueue(CancellationToken cancellationToken)
    {
        var queue = await _db.AuthorizationRequests
            .Include(x => x.Patient)
            .AsNoTracking()
            .Where(x => x.Status == AuthorizationStatus.Submitted || x.Status == AuthorizationStatus.InReview || x.Status == AuthorizationStatus.PendingInformation)
            .OrderByDescending(x => x.Priority == PriorityLevel.Stat)
            .ThenByDescending(x => x.Priority == PriorityLevel.Urgent)
            .ThenBy(x => x.DueDate)
            .Select(x => x.ToListItem())
            .ToListAsync(cancellationToken);

        return Ok(queue);
    }

    [HttpPost("{id:int}/decision")]
    public async Task<IActionResult> Decide(int id, ReviewDecisionRequest request, CancellationToken cancellationToken)
    {
        await _workflowService.ReviewAsync(id, request.Decision, request.Reason, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
        return Accepted();
    }
}
