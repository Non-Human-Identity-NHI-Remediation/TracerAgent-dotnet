namespace TracerAgent.Core.Services;

using Microsoft.Extensions.Logging;
using TracerAgent.Core.Interfaces;
using TracerAgent.Core.Models;

/// <summary>
/// Full Agent A pipeline:
///
///   1. Dep 1 — Activity Verification (SIEM → LDAP fallback → confidence)
///   2. Reclassification check:
///      - Recent activity within threshold? → Reclassify Active, flag IGA gap, DROP
///      - Otherwise → confirmed Stale/Orphaned, CONTINUE
///   3. ALL confirmed Stale/Orphaned continue — confidence is NOT a gate
///   4. Dep 2 — Context Resolution (AppCatalog: app status, owner, team)
///   5. Build case file
///   6. Route to BOTH Agent B (risk) AND Agent C (outreach)
///
/// Key principle: A low-confidence case file with Agent C's owner response
/// saying "yes, we still use this daily" is more valuable than a high-confidence
/// SIEM-corroborated one — because it uncovered a visibility gap.
/// </summary>
public sealed class EnrichmentOrchestrator : IEnrichmentOrchestrator
{
    private readonly IActivityVerifier _activityVerifier;
    private readonly IContextResolver _contextResolver;
    private readonly ILogger<EnrichmentOrchestrator> _log;

    public EnrichmentOrchestrator(
        IActivityVerifier activityVerifier,
        IContextResolver contextResolver,
        ILogger<EnrichmentOrchestrator> log)
    {
        _activityVerifier = activityVerifier;
        _contextResolver = contextResolver;
        _log = log;
    }

    public async Task<InvestigationResult> InvestigateAccountAsync(
        NhiAccount account, string requestId, CancellationToken ct = default)
    {
        _log.LogInformation(
            "═══ Investigation start: {Id} (upstream classification: {Class}) ═══",
            account.AccountId, account.Classification);

        // ── Dep 1: Activity Verification ─────────────────────────
        var verification = await _activityVerifier.VerifyAsync(account, ct);

        // ── Reclassification check ───────────────────────────────
        // Only override: if SIEM/LDAP found recent activity within the
        // app-specific threshold → IGA was wrong → reclassify Active + drop
        var (reclassified, reclassReason) = CheckReclassification(account, verification);

        if (reclassified)
        {
            _log.LogWarning(
                "⚡ {Id}: RECLASSIFIED → Active. IGA data gap. Full pipeline continues.",
                account.AccountId);
        }
        else
        {
            _log.LogInformation(
                "{Id}: Confirmed {Class} | {Confidence} confidence | continuing pipeline",
                account.AccountId, account.Classification, verification.Confidence);
        }

        // ── Dep 2: Context Resolution (AppCatalog) ───────────────
        var appContext = await _contextResolver.ResolveAsync(account, ct);

        // ── Build case file + routing ────────────────────────────
        var finalClassification = reclassified ? Classification.Active : account.Classification;
        var routing = BuildRouting(finalClassification, reclassified);

        var result = new InvestigationResult
        {
            AccountId = account.AccountId,
            RequestId = requestId,
            AccountData = account,
            FinalClassification = finalClassification,
            WasReclassified = reclassified,
            ReclassificationReason = reclassReason,
            ActivityVerification = verification,
            ApplicationContext = appContext,
            Routing = routing,
            InvestigatedAt = DateTime.UtcNow
        };

        _log.LogInformation(
            "═══ Investigation complete: {Id} → {Class} | {Confidence} | → B:{B} C:{C} | Goal: {Goal} ═══",
            result.AccountId, result.FinalClassification, verification.Confidence,
            routing.SendToAgentB, routing.SendToAgentC, routing.OutreachGoal);

        return result;
    }

    public async Task<IReadOnlyList<InvestigationResult>> InvestigateBatchAsync(
        InvestigationRequest request, CancellationToken ct = default)
    {
        _log.LogInformation(
            "Batch: {Count} accounts (request {Req})",
            request.Accounts.Count, request.RequestId);

        var semaphore = new SemaphoreSlim(5);
        var tasks = request.Accounts.Select(async account =>
        {
            await semaphore.WaitAsync(ct);
            try { return await InvestigateAccountAsync(account, request.RequestId, ct); }
            finally { semaphore.Release(); }
        });

        var results = await Task.WhenAll(tasks);

        var reclass = results.Count(r => r.WasReclassified);
        var stale = results.Count(r => r.FinalClassification == Classification.Stale);
        var orphaned = results.Count(r => r.FinalClassification == Classification.Orphaned);

        _log.LogInformation(
            "Batch complete: {Reclass} reclassified→Active, {Stale} stale, {Orphaned} orphaned → Agent B+C",
            reclass, stale, orphaned);

        return results;
    }

    // ─────────────────────────────────────────────────────────────
    // Reclassification: only if recent activity found within threshold
    // ─────────────────────────────────────────────────────────────
    private static (bool Reclassified, string? Reason) CheckReclassification(
        NhiAccount account, ActivityVerificationResult verification)
    {
        if (!verification.ActivityFound || !verification.LastConfirmedActivity.HasValue)
            return (false, null);

        var daysSince = (DateTime.UtcNow - verification.LastConfirmedActivity.Value).TotalDays;

        if (daysSince <= account.InactivityThresholdDays)
        {
            return (true,
                $"Reclassified → Active. {verification.VerifiedBy} shows activity " +
                $"{daysSince:F0}d ago (threshold: {account.InactivityThresholdDays}d). " +
                $"IGA incorrectly classified as {account.Classification} — data gap flagged.");
        }

        return (false, null);
    }

    // ─────────────────────────────────────────────────────────────
    // ALL accounts route to Agent B + Agent C. No exceptions.
    // Reclassified accounts get a specific outreach goal about the IGA gap.
    // ─────────────────────────────────────────────────────────────
    private static DownstreamRouting BuildRouting(Classification classification, bool wasReclassified) =>
        (classification, wasReclassified) switch
        {
            (Classification.Active, true) => new DownstreamRouting
            {
                SendToAgentB = true,
                SendToAgentC = true,
                OutreachGoal = "IGA data gap detected — account is active but IGA classified it incorrectly. " +
                               "Confirm usage with owner. Assess risk of IGA visibility gap."
            },
            (Classification.Stale, _) => new DownstreamRouting
            {
                SendToAgentB = true,
                SendToAgentC = true,
                OutreachGoal = "Get approval to disable account. Confirm usage details with owner."
            },
            (Classification.Orphaned, _) => new DownstreamRouting
            {
                SendToAgentB = true,
                SendToAgentC = true,
                OutreachGoal = "Transfer ownership. Confirm application status and usage with app team."
            },
            _ => new DownstreamRouting
            {
                SendToAgentB = true,
                SendToAgentC = true,
                OutreachGoal = "Review account status."
            }
        };
}