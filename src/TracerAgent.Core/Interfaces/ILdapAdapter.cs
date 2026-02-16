namespace TracerAgent.Core.Interfaces;

using TracerAgent.Core.Models;

/// <summary>
/// Dep 1, Step 2 — OpenLDAP (fallback).
/// Queries bind logs, access logs. Only called when SIEM returns no data.
/// </summary>
public interface ILdapAdapter
{
    Task<IReadOnlyList<ActivityRecord>> GetActivityAsync(
        string accountId,
        DateTime lookbackFrom,
        DateTime? lookbackTo = null,
        int maxRecords = 100,
        CancellationToken ct = default);
}