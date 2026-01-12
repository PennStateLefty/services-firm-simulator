var builder = DistributedApplication.CreateBuilder(args);

// Add Redis for Dapr state store and pub/sub
var redis = builder.AddRedis("redis");

// Add services with Dapr sidecars
var employeeService = builder.AddProject<Projects.EmployeeService>("employeeservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "employeeservice",
        AppPort = 8080,
        DaprHttpPort = 3500,
        DaprGrpcPort = 50001
    });

var onboardingService = builder.AddProject<Projects.OnboardingService>("onboardingservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "onboardingservice",
        AppPort = 8080,
        DaprHttpPort = 3501,
        DaprGrpcPort = 50002
    });

var performanceService = builder.AddProject<Projects.PerformanceService>("performanceservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "performanceservice",
        AppPort = 8080,
        DaprHttpPort = 3502,
        DaprGrpcPort = 50003
    });

var meritService = builder.AddProject<Projects.MeritService>("meritservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "meritservice",
        AppPort = 8080,
        DaprHttpPort = 3503,
        DaprGrpcPort = 50004
    });

var offboardingService = builder.AddProject<Projects.OffboardingService>("offboardingservice")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "offboardingservice",
        AppPort = 8080,
        DaprHttpPort = 3504,
        DaprGrpcPort = 50005
    });

// Build and run
var app = builder.Build();
await app.RunAsync();
