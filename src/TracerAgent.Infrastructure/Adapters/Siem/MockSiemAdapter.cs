namespace TracerAgent.Infrastructure.Adapters.Siem;

using TracerAgent-dotnet.TracerAgent.Core.Interfaces;
using TracerAgent-dotnet.TracerAgent.Core.Models;

/// <summary>
/// Mock SIEM (Splunk) — Dep 1, Step 1.
///
/// Scenario coverage:
///   SVC-SAP-001 → ❌ No SIEM data        (falls to LDAP)
///   SVC-AWS-002 → ✅ Recent SIEM activity (triggers reclassification → Active)
///   SVC-AZ-003  → ❌ No SIEM data         (falls to LDAP, which also empty → Low)
///   SVC-GCP-004 → ❌ No SIEM data         (falls to LDAP, which has old data → Medium)
///   SVC-MF-005  → ✅ Old SIEM data        (High confidence, but beyond threshold → confirmed Orphaned)
/// </summary>
public sealed class MockSiemAdapter : ISiemAdapter
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
        "SVC-SAP-001" => [],    // No SIEM data → falls to LDAP

        // Recent: last activity ~hours ago → will trigger reclassification
        "SVC-AWS-002" => Enumerable.Range(0, 20).Select(i => new ActivityRecord
        {
            AccountId = accountId,
            Source = "Splunk",
            Timestamp = DateTime.UtcNow.AddHours(-i * 6),
            EventType = i % 2 == 0 ? "DeploymentTrigger" : "APICall",
            TargetResource = "AWS_CodePipeline",
            SourceIp = "10.0.2.100"
        }),

        "SVC-AZ-003" => [],     // No SIEM data → falls to LDAP (also empty)

        "SVC-GCP-004" => [],    // No SIEM data → falls to LDAP

        // Old: last activity ~300 days ago → High confidence but beyond 180d threshold
        "SVC-MF-005" => Enumerable.Range(0, 5).Select(i => new ActivityRecord
        {
            AccountId = accountId,
            Source = "Splunk",
            Timestamp = DateTime.UtcNow.AddDays(-300).AddDays(-i * 30),
            EventType = "PayrollBatch",
            TargetResource = "MF_PAYROLL_SYS",
            SourceIp = "10.0.0.10"
        }),

        _ => []
    };
}