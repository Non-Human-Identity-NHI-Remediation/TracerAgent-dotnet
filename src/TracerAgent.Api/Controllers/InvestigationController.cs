namespace TracerAgent.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using TracerAgent-dotnet.TracerAgent.Core.Models;
using TracerAgent-dotnet.TracerAgent.Infrastructure.Queue;

[ApiController]
[Route("api/[controller]")]
public class InvestigationController : ControllerBase
{
    private readonly InvestigationChannel _channel;

    public InvestigationController(InvestigationChannel channel) => _channel = channel;

    /// <summary>
    /// IAM engineer submits pre-classified Stale/Orphaned accounts for investigation.
    /// Returns 202 — processing is async via background worker.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] InvestigationRequest request, CancellationToken ct)
    {
        if (request.Accounts is not { Count: > 0 })
            return BadRequest(new { error = "At least one account is required." });

        // Agent A only processes Stale or Orphaned — Active should never arrive
        var invalid = request.Accounts
            .Where(a => a.Classification == Classification.Active)
            .Select(a => a.AccountId)
            .ToList();

        if (invalid.Count > 0)
            return BadRequest(new
            {
                error = "Agent A only processes Stale or Orphaned accounts. Active accounts rejected.",
                rejectedAccountIds = invalid
            });

        await _channel.EnqueueAsync(request, ct);

        return Accepted(new
        {
            requestId = request.RequestId,
            accountCount = request.Accounts.Count,
            staleCount = request.Accounts.Count(a => a.Classification == Classification.Stale),
            orphanedCount = request.Accounts.Count(a => a.Classification == Classification.Orphaned),
            queueDepth = _channel.PendingCount,
            message = "Queued. Accounts will be verified (SIEM→LDAP), enriched (AppCatalog), and routed to Agent B + C."
        });
    }

    [HttpGet("status")]
    public IActionResult Status() => Ok(new
    {
        pendingInvestigations = _channel.PendingCount,
        status = "running"
    });

}