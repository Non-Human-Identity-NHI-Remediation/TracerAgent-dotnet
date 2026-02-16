namespace TracerAgent.Core.Interfaces;

using TracerAgent.Core.Models;

/// <summary>
/// Dep 1, Step 1 — SIEM (Splunk).
/// Queries auth logs, API call logs, service execution logs.
/// </summary>
public interface ISiemAdapter
{
    Task<IReadOnlyList<ActivityRecord>> GetActivityAsync(
        string accountId,
        DateTime lookbackFrom,
        DateTime? lookbackTo = null,
        int maxRecords = 100,
        CancellationToken ct = default);
}