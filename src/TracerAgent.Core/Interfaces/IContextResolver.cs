namespace TracerAgent.Core.Interfaces;

using TracerAgent.Core.Models;

/// <summary>
/// Dep 2 — Context Resolution.
/// Queries AppCatalog/CMDB for: app status, owner, team contacts.
/// </summary>
public interface IContextResolver
{
    Task<AppContext> ResolveAsync(
        NHIAccount account,
        CancellationToken ct = default);
}