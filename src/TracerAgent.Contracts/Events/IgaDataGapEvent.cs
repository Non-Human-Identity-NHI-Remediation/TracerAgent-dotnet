namespace TracerAgent.Contracts.Events;

/// <summary>
/// Published when an account is reclassified → Active because
/// SIEM/LDAP found recent activity that IGA missed.
/// Feed back to SailPoint/Saviynt for reconciliation.
/// </summary>
public sealed record IgaDataGapEvent
{
    public required string EventId { get; init; }
    public required string AccountId { get; init; }
    public required string VerifiedBy { get; init; }
    public required DateTime LastConfirmedActivity { get; init; }
    public required string OriginalClassification { get; init; }
    public required string Details { get; init; }
    public required DateTime PublishedAt { get; init; }
}