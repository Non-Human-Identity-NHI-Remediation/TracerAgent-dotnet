namespace TracerAgent.Core.Services;

using Microsoft.Extensions.Logging;
using TracerAgent.Core.Interfaces;
using TracerAgent.Core.Models;

/// <summary>
/// Dep 1 — Activity Verification.
/// Two-step fallback chain (NOT parallel):
///
///   Step 1: Query SIEM (Splunk) → found? → 🟢 High confidence, done
///                                → no data → Step 2
///   Step 2: Query OpenLDAP      → found? → 🟡 Medium confidence, done
///                                → no data → 🔴 Low confidence, flag
///
/// Confidence is a DATA QUALITY SIGNAL, not a gate.
/// All accounts continue the full pipeline regardless.
/// </summary>
public sealed class ActivityVerifier : IActivityVerifier
{
    private readonly ISiemAdapter _siem;
    private readonly ILdapAdapter _ldap;
    private readonly ILogger<ActivityVerifier> _log;

    private const int LookbackDays = 365;

    public ActivityVerifier(ISiemAdapter siem, ILdapAdapter ldap, ILogger<ActivityVerifier> log)
    {
        _siem = siem;
        _ldap = ldap;
        _log = log;
    }

    public async Task<ActivityVerificationResult> VerifyAsync(
        NhiAccount account, CancellationToken ct = default)
    {
        var lookbackFrom = DateTime.UtcNow.AddDays(-LookbackDays);

        // ── Step 1: SIEM (Splunk) ────────────────────────────────
        _log.LogInformation("Dep1/Step1: Querying SIEM for {Id}", account.AccountId);
        var siemRecords = await _siem.GetActivityAsync(account.AccountId, lookbackFrom, ct: ct);

        if (siemRecords.Count > 0)
        {
            var last = siemRecords.MaxBy(r => r.Timestamp)!;
            _log.LogInformation(
                "{Id}: SIEM → {Count} events, last {Last} → 🟢 High",
                account.AccountId, siemRecords.Count, last.Timestamp);

            return new ActivityVerificationResult
            {
                AccountId = account.AccountId,
                Confidence = ConfidenceLevel.High,
                ActivityFound = true,
                LastConfirmedActivity = last.Timestamp,
                VerifiedBy = "Splunk",
                Evidence = siemRecords,
                Summary = $"SIEM corroborated: {siemRecords.Count} event(s). Last activity {last.Timestamp:u} ({last.EventType})."
            };
        }

        // ── Step 2: OpenLDAP (fallback — only if SIEM empty) ────
        _log.LogInformation("Dep1/Step2: SIEM empty → falling back to OpenLDAP for {Id}", account.AccountId);
        var ldapRecords = await _ldap.GetActivityAsync(account.AccountId, lookbackFrom, ct: ct);

        if (ldapRecords.Count > 0)
        {
            var last = ldapRecords.MaxBy(r => r.Timestamp)!;
            _log.LogInformation(
                "{Id}: LDAP → {Count} events, last {Last} → 🟡 Medium",
                account.AccountId, ldapRecords.Count, last.Timestamp);

            return new ActivityVerificationResult
            {
                AccountId = account.AccountId,
                Confidence = ConfidenceLevel.Medium,
                ActivityFound = true,
                LastConfirmedActivity = last.Timestamp,
                VerifiedBy = "OpenLDAP",
                Evidence = ldapRecords,
                Summary = $"LDAP verified: {ldapRecords.Count} event(s). Last activity {last.Timestamp:u} ({last.EventType}). SIEM had no data."
            };
        }

        // ── No data from any source ──────────────────────────────
        _log.LogWarning("{Id}: No data from SIEM or LDAP → 🔴 Low", account.AccountId);

        return new ActivityVerificationResult
        {
            AccountId = account.AccountId,
            Confidence = ConfidenceLevel.Low,
            ActivityFound = false,
            LastConfirmedActivity = null,
            VerifiedBy = null,
            Evidence = [],
            Summary = "No activity data from any source (SIEM or OpenLDAP). Unable to verify. Account continues through pipeline for Agent B/C enrichment — IAM team will decide."
        };
    }
}
