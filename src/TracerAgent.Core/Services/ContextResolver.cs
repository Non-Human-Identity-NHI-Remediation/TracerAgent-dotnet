namespace TracerAgent.Core.Services;

using Microsoft.Extensions.Logging;
using TracerAgent.Core.Interfaces;
using TracerAgent.Core.Models;

/// <summary>
/// Dep 2 — Context Resolution.
/// Queries AppCatalog/CMDB (NOT IGA) for:
///   1. Which app uses this account?
///   2. App still active or decommissioned?
///   3. App owner / team contacts
/// </summary>
public sealed class ContextResolver : IContextResolver
{
    private readonly IAppCatalogAdapter _appCatalog;
    private readonly ILogger<ContextResolver> _log;

    public ContextResolver(IAppCatalogAdapter appCatalog, ILogger<ContextResolver> log)
    {
        _appCatalog = appCatalog;
        _log = log;
    }

    public async Task<AppContext> ResolveAsync(NhiAccount account, CancellationToken ct = default)
    {
        _log.LogInformation("Dep2: Resolving context for {Id} → app {AppId}",
            account.AccountId, account.ApplicationId);

        var ctx = await _appCatalog.GetAppContextAsync(account.ApplicationId, ct);

        if (ctx is not null)
        {
            _log.LogInformation(
                "Dep2: {AppId} → status={Status}, owner={Owner}, team={Team}",
                ctx.ApplicationId, ctx.Status, ctx.AppOwnerName ?? "unknown", ctx.TeamName ?? "unknown");
            return ctx;
        }

        // AppCatalog didn't have this app — build a best-effort response
        _log.LogWarning("Dep2: App {AppId} not found in AppCatalog. Using account metadata.", account.ApplicationId);

        return new AppContext
        {
            ApplicationId = account.ApplicationId,
            ApplicationName = account.ApplicationName,
            Platform = account.Platform,
            Status = AppStatus.Unknown,
            Notes = "Application not found in AppCatalog/CMDB. Manual verification required."
        };
    }
}