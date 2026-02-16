using TracerAgent-dotnet.TracerAgent.Core.Interfaces;
using TracerAgent-dotnet.TracerAgent.Core.Services;
using TracerAgent-dotnet.TracerAgent.Infrastructure.Adapters.AppCatalog;
using TracerAgent-dotnet.TracerAgent.Infrastructure.Adapters.Ldap;
using TracerAgent-dotnet.TracerAgent.Infrastructure.Adapters.Siem;
using TracerAgent-dotnet.TracerAgent.Infrastructure.Queue;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Queue ────────────────────────────────────────────────────
builder.Services.AddSingleton<InvestigationChannel>();
builder.Services.AddHostedService<InvestigationWorker>();

// ── Core services ────────────────────────────────────────────
builder.Services.AddScoped<IActivityVerifier, ActivityVerifier>();
builder.Services.AddScoped<IContextResolver, ContextResolver>();
builder.Services.AddScoped<IEnrichmentOrchestrator, EnrichmentOrchestrator>();

// ── Adapters (3 total — locked inventory) ────────────────────
// Dep 1, Step 1: SIEM (Splunk)
builder.Services.AddScoped<ISiemAdapter, MockSiemAdapter>();
// Dep 1, Step 2: OpenLDAP (fallback)
builder.Services.AddScoped<ILdapAdapter, MockLdapAdapter>();
// Dep 2: AppCatalog/CMDB
builder.Services.AddScoped<IAppCatalogAdapter, MockAppCatalogAdapter>();

// NO IGA adapter — IGA is upstream. Accounts arrive pre-classified.

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();