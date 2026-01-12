using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add Redis for Dapr state store and pub/sub
var redis = builder.AddRedis("redis");

// Note: Aspire project references require proper SDK setup
// For now, services will be run via Dapr directly
Console.WriteLine("Aspire AppHost configured");
Console.WriteLine("Run services with Dapr CLI: dapr run --app-id <service-name> --app-port 8080 -- dotnet run");

var app = builder.Build();
await app.RunAsync();
