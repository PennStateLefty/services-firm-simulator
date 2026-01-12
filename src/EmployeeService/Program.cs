using EmployeeService.Infrastructure;
using EmployeeService.Services;
using Dapr.Client;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add Dapr client
builder.Services.AddDaprClient();
builder.Services.AddSingleton<IDaprStateStore, DaprStateStore>();

// Add services
builder.Services.AddScoped<IDepartmentService, DepartmentService>();

// Add controllers
builder.Services.AddControllers().AddDapr();

// Add CORS
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

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("employeeservice"))
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

// Add OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Dapr pub/sub subscriptions
app.MapSubscribeHandler();

app.Run();
