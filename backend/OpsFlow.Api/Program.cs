using FluentValidation;
using OpsFlow.Api.Behaviours;
using OpsFlow.Api.Extensions;
using OpsFlow.Api.Features.Auth.Login;
using OpsFlow.Api.Features.Auth.Logout;
using OpsFlow.Api.Features.Auth.Refresh;
using OpsFlow.Api.Features.Health.GetStatus;
using OpsFlow.Api.Features.Inventory;
using OpsFlow.Api.Features.Regions;
using OpsFlow.Api.Features.Stores;
using OpsFlow.Api.Features.Checklists;
using OpsFlow.Api.Features.RecurringAssignments;
using OpsFlow.Api.Features.Dashboard;
using OpsFlow.Api.Features.DepositLog;
using OpsFlow.Api.Features.FormSubmissions;
using OpsFlow.Api.Features.FormTemplates;
using OpsFlow.Api.Features.Me;
using OpsFlow.Api.Features.StoreSettings;
using OpsFlow.Api.Features.Tasks;
using OpsFlow.Api.Features.TenantSettings;
using OpsFlow.Api.Features.Templates;
using OpsFlow.Api.Features.Users;
using OpsFlow.Api.Hubs;
using OpsFlow.Api.Jobs;
using OpsFlow.Api.Services;
using Quartz;
using Scalar.AspNetCore;

// Load .env file from the workspace root (walk up from the running directory)
var envDir = Directory.GetCurrentDirectory();
for (var i = 0; i < 6; i++)
{
    var candidate = Path.Combine(envDir, ".env");
    if (File.Exists(candidate))
    {
        foreach (var line in File.ReadAllLines(candidate))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
            var sep = trimmed.IndexOf('=');
            if (sep < 1) continue;
            var key = trimmed[..sep].Trim();
            var val = trimmed[(sep + 1)..].Trim();
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, val);
        }
        break;
    }
    var parent = Directory.GetParent(envDir);
    if (parent is null) break;
    envDir = parent.FullName;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// MediatR — scans this assembly for all handlers
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
});

// FluentValidation — scans this assembly for all validators
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly, includeInternalTypes: true);

// Infrastructure adapters (Supabase dev / Azure prod)
builder.Services.AddInfrastructure(builder.Configuration);

// Auth
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TokenService>();

// SignalR
builder.Services.AddSignalR();

// System template seeder — runs once at startup per active tenant
builder.Services.AddHostedService<TemplateSeederService>();

// Quartz — background jobs
builder.Services.AddQuartz(q =>
{
    var generateKey = new JobKey("GenerateTaskInstancesJob");
    q.AddJob<GenerateTaskInstancesJob>(opts => opts.WithIdentity(generateKey));
    q.AddTrigger(opts => opts
        .ForJob(generateKey)
        .WithIdentity("GenerateTaskInstancesTrigger")
        .WithCronSchedule("0 0/15 * * * ?")); // every 15 min

    var overdueKey = new JobKey("OverduePromotionJob");
    q.AddJob<OverduePromotionJob>(opts => opts.WithIdentity(overdueKey));
    q.AddTrigger(opts => opts
        .ForJob(overdueKey)
        .WithIdentity("OverduePromotionTrigger")
        .WithCronSchedule("0 0/5 * * * ?")); // every 5 min

    var deferKey = new JobKey("ActivateDeferredTasksJob");
    q.AddJob<ActivateDeferredTasksJob>(opts => opts.WithIdentity(deferKey));
    q.AddTrigger(opts => opts
        .ForJob(deferKey)
        .WithIdentity("ActivateDeferredTasksTrigger")
        .WithCronSchedule("0 0 6 * * ?")); // daily 6:00 AM
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// CORS — dev origins plus any deployed frontend origins from CORS_ALLOWED_ORIGINS
// (comma-separated, e.g. "https://opsflow.vercel.app,https://opsflow-git-main.vercel.app").
var corsOrigins = new List<string> { "http://localhost:4200", "http://localhost:4201" };
var extraOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
if (!string.IsNullOrWhiteSpace(extraOrigins))
{
    corsOrigins.AddRange(extraOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

builder.Services.AddCors(opts =>
    opts.AddPolicy("AppCors", p =>
        p.WithOrigins(corsOrigins.ToArray())
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()));

var app = builder.Build();

app.UseCors("AppCors");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        context.Response.ContentType = "application/json";
        if (ex is UnauthorizedAccessException)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        else if (ex is ValidationException vex)
        {
            context.Response.StatusCode = 400;
            var errors = vex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            await context.Response.WriteAsJsonAsync(new { message = "Validation failed.", errors });
        }
        else
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred." });
        }
    });
});

app.UseAuthentication();
app.UseAuthorization();

// Map all Vertical Slice endpoints
app.MapHealthEndpoints();
app.MapLoginEndpoint();
app.MapRefreshEndpoint();
app.MapLogoutEndpoint();
app.MapRegionsEndpoints();
app.MapStoresEndpoints();
app.MapUsersEndpoints();
app.MapTemplatesEndpoints();
app.MapChecklistsEndpoints();
app.MapRecurringAssignmentsEndpoints();
app.MapTasksEndpoints();
app.MapInventoryEndpoints();
app.MapStoreSettingsEndpoints();
app.MapDepositLogEndpoints();
app.MapDashboardEndpoints();
app.MapMeEndpoints();
app.MapTenantSettingsEndpoints();
app.MapFormTemplatesEndpoints();
app.MapFormSubmissionsEndpoints();
app.MapHub<TaskBoardHub>("/hubs/taskboard");

app.Run();

// Needed for WebApplicationFactory in integration tests
public partial class Program { }
