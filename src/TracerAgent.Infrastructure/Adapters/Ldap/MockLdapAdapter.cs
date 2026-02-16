namespace TracerAgent.Infrastructure.Adapters.Ldap;

using TracerAgent-dotnet.TracerAgent.Core.Interfaces;
using TracerAgent-dotnet.TracerAgent.Core.Models;

/// <summary>
/// Mock OpenLDAP — Dep 1, Step 2 (fallback, only called when SIEM empty).
///
/// Scenario coverage (only for accounts where SIEM returned nothing):
///   SVC-SAP-001 → ✅ Old LDAP bind logs (Medium confidence, beyond 90d → confirmed Stale)
///   SVC-AZ-003  → ❌ No LDAP data either (Low confidence → still continues pipeline)
///   SVC-GCP-004 → ✅ Old LDAP bind logs (Medium confidence, beyond 30d → confirmed Stale)
/// </summary>
public sealed class MockLdapAdapter : ILdapAdapter
{
    public Task<IReadOnlyList<ActivityRecord>> GetActivityAsync(
        string accountId, DateTime lookbackFrom, DateTime? lookbackTo = null,
        int maxRecords = 100, CancellationToken ct = default)
    {
        var to = lookbackTo ?? DateTime.UtcNow;
        var records = GenerateActivity(accountId)
            .Where(a => a.Timestamp >= lookbackFrom && a.Timestamp <= to)
            .OrderByDescending(a => a.Timestamp)
            .Take(maxRecords)
            .ToList();

        return Task.FromResult<IReadOnlyList<ActivityRecord>>(records);
    }

    private static IEnumerable<ActivityRecord> GenerateActivity(string accountId) => accountId switch
    {
        // Old bind logs ~130d ago → beyond 90d SAP threshold → confirmed Stale
        "SVC-SAP-001" => Enumerable.Range(0, 8).Select(i => new ActivityRecord
        {
            AccountId = accountId,
            Source = "OpenLDAP",
            Timestamp = DateTime.UtcNow.AddDays(-130).AddDays(-i * 7),
            EventType = "LDAPBind",
            TargetResource = "cn=svc_sap_batch,ou=services,dc=company",
            SourceIp = "10.0.1.50"
        }),

        "SVC-AZ-003" => [],     // Nothing here either → Low confidence

        // Old bind logs ~60d ago → beyond 30d GCP threshold → confirmed Stale
        "SVC-GCP-004" => Enumerable.Range(0, 4).Select(i => new ActivityRecord
        {
            AccountId = accountId,
            Source = "OpenLDAP",
            Timestamp = DateTime.UtcNow.AddDays(-60).AddDays(-i * 10),
            EventType = "LDAPBind",
            TargetResource = "cn=svc_gcp_etl,ou=services,dc=company",
            SourceIp = "10.0.3.75"
        }),

        _ => []
    };
}