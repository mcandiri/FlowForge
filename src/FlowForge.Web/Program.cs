using FlowForge.Core.Extensions;
using FlowForge.Web.Components;
using FlowForge.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── FlowForge Core services (NodeRegistry, WorkflowEngine, Templates, Exporters) ──
builder.Services.AddFlowForgeCore();

// ── FlowForge Web services ──────────────────────────────
builder.Services.AddScoped<CanvasStateService>();
builder.Services.AddScoped<WorkflowExecutionService>();
builder.Services.AddScoped<ExportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
