namespace TracerAgent.Infrastructure.Queue;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TracerAgent-dotnet.TracerAgent.Core.Interfaces;
using TracerAgent-dotnet.TracerAgent.Core.Models;

public sealed class InvestigationWorker : BackgroundService
{
    private readonly InvestigationChannel _channel;
    private readonly IEnrichmentOrchestrator _orchestrator;
    private readonly ILogger<InvestigationWorker> _log;

    public InvestigationWorker(
        InvestigationChannel channel,
        IEnrichmentOrchestrator orchestrator,
        ILogger<InvestigationWorker> log)
    {
        _channel = channel;
        _orchestrator = orchestrator;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("InvestigationWorker started. Awaiting work items...");

        await foreach (var request in _channel.ReadAllAsync(ct))
        {
            try
            {
                _log.LogInformation("Processing request {Id}: {Count} accounts",
                    request.RequestId, request.Accounts.Count);

                var results = await _orchestrator.InvestigateBatchAsync(request, ct);

                foreach (var result in results)
                    await RouteResultAsync(result, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogError(ex, "Error processing request {Id}", request.RequestId);
            }
        }
    }

    private Task RouteResultAsync(InvestigationResult result, CancellationToken ct)
    {
        if (result.WasReclassified)
        {
            _log.LogInformation(
                "⚡ {Id}: Reclassified → Active. IGA data gap flagged. Dropped.",
                result.AccountId);
            // TODO: Publish IgaDataGapEvent
            return Task.CompletedTask;
        }

        // Both Agent B AND Agent C for ALL Stale/Orphaned
        _log.LogInformation(
            "→ Agent B (Risk) + Agent C (Outreach): {Id} ({Class}, {Confidence}) — {Goal}",
            result.AccountId, result.FinalClassification,
            result.ActivityVerification.Confidence, result.Routing.OutreachGoal);

        // TODO: Publish AccountEnrichedEvent to Agent B queue
        // TODO: Publish AccountEnrichedEvent to Agent C queue

        return Task.CompletedTask;
    }
}