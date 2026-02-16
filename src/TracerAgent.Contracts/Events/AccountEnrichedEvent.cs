namespace TracerAgent.Contracts.Events;

using TracerAgent.Models;

/// <summary>
/// Published per account → consumed by BOTH Agent B (risk) AND Agent C (outreach).
/// ALL Stale/Orphaned accounts get this event regardless of confidence level.
/// </summary>
public sealed record AccountEnrichedEvent
{
    public required string EventId { get; init; }
    public required InvestigationResult Result { get; init; }
    public required DateTime PublishedAt { get; init; }
}