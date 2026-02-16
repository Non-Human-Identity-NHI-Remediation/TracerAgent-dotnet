namespace TracerAgent.Core.Interfaces;

using TracerAgent.Core.Models;

/// <summary>
/// Dep 2 — App Catalog / CMDB (e.g., ServiceNow).
/// Resolves application context: status, owner, team contacts.
/// Mocked initially, real integration later.
/// </summary>
public interface IAppCatalogAdapter
{
    Task<AppContext?> GetAppContextAsync(
        string applicationId,
        CancellationToken ct = default);
}