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
        var tag = result.WasReclassified ? "⚡ RECLASSIFIED" : "✓";

        // Every account goes to Agent B + Agent C. No exceptions.
        _log.LogInformation(
            "{Tag} → Agent B (Risk) + Agent C (Outreach): {Id} ({Class}, {Confidence}) — {Goal}",
            tag, result.AccountId, result.FinalClassification,
            result.ActivityVerification.Confidence, result.Routing.OutreachGoal);

        if (result.WasReclassified)
        {
            // Also publish IGA data gap event for reconciliation
            _log.LogWarning(
                "⚡ IGA Data Gap: {Id} was {Original} in IGA but verified Active by {Source}",
                result.AccountId, result.AccountData.Classification,
                result.ActivityVerification.VerifiedBy);
            // TODO: Publish IgaDataGapEvent
        }

        // TODO: Publish AccountEnrichedEvent to Agent B queue
        // TODO: Publish AccountEnrichedEvent to Agent C queue

        return Task.CompletedTask;
    }
}
