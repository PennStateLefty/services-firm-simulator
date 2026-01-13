using OnboardingService.Infrastructure;
using OnboardingService.Models;
using OnboardingService.Services;
using Dapr.Client;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configure onboarding task templates from appsettings.json
// Templates define the default tasks that are automatically created for each new onboarding case
// Configuration can be modified in appsettings.json without recompiling
// Future enhancement: Consider moving to database/state store for runtime editability
builder.Services.Configure<List<TaskTemplate>>(
    builder.Configuration.GetSection("OnboardingTaskTemplates"));

builder.Services.AddDaprClient();
builder.Services.AddSingleton<IDaprStateStore, DaprStateStore>();
builder.Services.AddScoped<IOnboardingService, OnboardingServiceImpl>();
builder.Services.AddScoped<ITaskTemplateService, TaskTemplateService>();
builder.Services.AddScoped<IEmployeeValidationService, EmployeeValidationService>();
builder.Services.AddControllers().AddDapr();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("onboardingservice"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Dapr.*")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("Dapr.*")
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(logging => logging.AddOtlpExporter());
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapSubscribeHandler();

app.Run();
