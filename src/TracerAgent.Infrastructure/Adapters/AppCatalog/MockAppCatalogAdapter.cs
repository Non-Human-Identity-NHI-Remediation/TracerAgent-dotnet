namespace TracerAgent.Infrastructure.Adapters.AppCatalog;

using TracerAgent-dotnet.TracerAgent.Core.Interfaces;
using TracerAgent-dotnet.TracerAgent.Core.Models;

/// <summary>
/// Mock AppCatalog/CMDB — Dep 2 (mocked initially, real ServiceNow later).
/// Provides app status, owner, and team contacts for context resolution.
/// </summary>
public sealed class MockAppCatalogAdapter : IAppCatalogAdapter
{
    private static readonly Dictionary<string, AppContext> Catalog = new()
    {
        ["APP-SAP-ERP"] = new AppContext
        {
            ApplicationId = "APP-SAP-ERP",
            ApplicationName = "SAP ERP Production",
            Platform = "SAP",
            Status = AppStatus.Active,
            AppOwnerId = "EMP-2001",
            AppOwnerName = "David Park",
            AppOwnerEmail = "david.park@company.com",
            TeamName = "ERP Operations",
            TeamDistributionList = "erp-ops@company.com"
        },
        ["APP-AWS-CICD"] = new AppContext
        {
            ApplicationId = "APP-AWS-CICD",
            ApplicationName = "AWS CI/CD Pipeline",
            Platform = "AWS",
            Status = AppStatus.Active,
            AppOwnerId = "EMP-2002",
            AppOwnerName = "Lisa Tran",
            AppOwnerEmail = "lisa.tran@company.com",
            TeamName = "DevOps Platform",
            TeamDistributionList = "devops-platform@company.com"
        },
        ["APP-AZ-LEGACY"] = new AppContext
        {
            ApplicationId = "APP-AZ-LEGACY",
            ApplicationName = "Azure Legacy Sync",
            Platform = "Azure",
            Status = AppStatus.Deprecated,
            AppOwnerId = "EMP-2003",
            AppOwnerName = "James Rivera",
            AppOwnerEmail = "james.rivera@company.com",
            TeamName = "Legacy Systems",
            TeamDistributionList = "legacy-sys@company.com",
            Notes = "Scheduled for decommission Q2 2026. Migration to Azure Data Factory in progress."
        },
        ["APP-GCP-DATA"] = new AppContext
        {
            ApplicationId = "APP-GCP-DATA",
            ApplicationName = "GCP Data Lake",
            Platform = "GCP",
            Status = AppStatus.Active,
            AppOwnerId = "EMP-2004",
            AppOwnerName = "Anika Sharma",
            AppOwnerEmail = "anika.sharma@company.com",
            TeamName = "Data Engineering",
            TeamDistributionList = "data-eng@company.com"
        },
        ["APP-MF-PAYROLL"] = new AppContext
        {
            ApplicationId = "APP-MF-PAYROLL",
            ApplicationName = "Mainframe Payroll System",
            Platform = "Mainframe",
            Status = AppStatus.Active,
            AppOwnerId = "EMP-2005",
            AppOwnerName = "Robert Kim",
            AppOwnerEmail = "robert.kim@company.com",
            TeamName = "Payroll & HR Systems",
            TeamDistributionList = "payroll-systems@company.com",
            Notes = "Critical system — 24/7 on-call support."
        }
    };

    public Task<AppContext?> GetAppContextAsync(string applicationId, CancellationToken ct = default)
        => Task.FromResult(Catalog.GetValueOrDefault(applicationId));
}