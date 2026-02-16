namespace TracerAgent.Core.Interfaces;

using TracerAgent.Core.Models;

/// <summary>
/// Full Agent A pipeline:
/// 1. Dep 1: Activity Verification (SIEM → LDAP fallback → confidence)
/// 2. Reclassification check (recent activity? → Active + IGA gap flag + drop)
/// 3. ALL confirmed Stale/Orphaned continue (confidence ≠ gate)
/// 4. Dep 2: Context Resolution (AppCatalog — app, status, team)
/// 5. Build case file + route to BOTH Agent B + Agent C
/// </summary>
public interface IEnrichmentOrchestrator
{
    Task<InvestigationResult> InvestigateAccountAsync(
        NHIAccount account,
        string requestId,
        CancellationToken ct = default);

    Task<IReadOnlyList<InvestigationResult>> InvestigateBatchAsync(
        InvestigationRequest request,
        CancellationToken ct = default);
}