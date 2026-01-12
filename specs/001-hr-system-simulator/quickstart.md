# Quickstart Guide: HR System Simulator

**Last Updated**: 2026-01-09  
**Target Audience**: Developers setting up local development environment

## Overview

This guide walks you through setting up the HR System Simulator on your local machine using .NET Aspire for orchestration, Dapr for microservices infrastructure, and React for the frontend.

**Prerequisites**:
- .NET 10 SDK
- Node.js 20+ (for React frontend)
- Docker Desktop (for Aspire dependencies)
- Visual Studio 2022 or VS Code with C# Dev Kit

---

## Quick Start (5 Minutes)

### 1. Clone and Navigate

```bash
git clone <repository-url>
cd services-firm-simulator
```

### 2. Install Dapr CLI

```bash
# macOS/Linux
curl -fsSL https://raw.githubusercontent.com/dapr/cli/master/install/install.sh | /bin/bash

# Windows (PowerShell as Admin)
powershell -Command "iwr -useb https://raw.githubusercontent.com/dapr/cli/master/install/install.ps1 | iex"

# Verify installation
dapr --version

# Initialize Dapr locally (installs Redis, Zipkin)
dapr init
```

### 3. Restore Dependencies

```bash
# Restore .NET dependencies
dotnet restore

# Install frontend dependencies
cd frontend
npm install
cd ..
```

### 4. Run with Aspire

```bash
# Start all services via Aspire AppHost
dotnet run --project src/AppHost

# Output will show:
# - Aspire Dashboard URL: http://localhost:15000
# - Frontend URL: http://localhost:3000
# - Service URLs: http://localhost:5001, 5002, 5003, etc.
```

### 5. Open Application

- **Frontend**: http://localhost:3000
- **Aspire Dashboard**: http://localhost:15000 (observability)
- **API Documentation**: http://localhost:5001/swagger (Employee Service)

---

## Project Structure

```
services-firm-simulator/
├── src/
│   ├── AppHost/                    # Aspire orchestration
│   │   └── Program.cs              # Service configuration
│   ├── EmployeeService/            # Employee management
│   ├── OnboardingService/          # New hire onboarding
│   ├── PerformanceService/         # Performance reviews
│   ├── MeritService/               # Compensation adjustments
│   └── OffboardingService/         # Employee departures
├── frontend/                       # React application
│   ├── src/
│   │   ├── api/                    # API client logic
│   │   ├── components/             # Reusable components
│   │   ├── pages/                  # Route pages
│   │   └── hooks/                  # Custom React hooks
│   └── package.json
├── specs/                          # Feature specifications
│   └── 001-hr-system-simulator/
│       ├── spec.md                 # Requirements
│       ├── plan.md                 # Implementation plan
│       ├── research.md             # Technical decisions
│       ├── data-model.md           # Entity schemas
│       ├── contracts/              # OpenAPI specs
│       └── quickstart.md           # This file
└── tests/
    ├── EmployeeService.Tests/
    ├── PerformanceService.Tests/
    └── Contract.Tests/             # Pact consumer/provider tests
```

---

## Development Workflow

### Daily Development

1. **Start Services**:
   ```bash
   dotnet run --project src/AppHost
   ```

2. **Make Code Changes**:
   - .NET services: Hot reload enabled automatically
   - React frontend: Vite hot reload enabled

3. **View Logs and Traces**:
   - Open Aspire Dashboard: http://localhost:15000
   - Navigate to "Traces" for distributed tracing
   - Navigate to "Logs" for structured logs
   - Navigate to "Metrics" for performance data

4. **Run Tests**:
   ```bash
   # Run all tests
   dotnet test
   
   # Run specific service tests
   dotnet test tests/EmployeeService.Tests
   
   # Run contract tests
   dotnet test tests/Contract.Tests
   
   # Run frontend tests
   cd frontend && npm test
   ```

### Creating a New Service

1. **Create Service Project**:
   ```bash
   dotnet new webapi -n NewService -o src/NewService
   cd src/NewService
   dotnet add package Dapr.AspNetCore
   ```

2. **Register in AppHost**:
   ```csharp
   // src/AppHost/Program.cs
   var newService = builder.AddProject<Projects.NewService>("new-service")
       .WithDaprSidecar()
       .WithReference(redis);
   ```

3. **Configure Dapr**:
   ```csharp
   // src/NewService/Program.cs
   var builder = WebApplication.CreateBuilder(args);
   builder.AddServiceDefaults(); // Aspire defaults
   builder.Services.AddControllers().AddDapr();
   
   var app = builder.Build();
   app.MapControllers();
   app.MapSubscribeHandler(); // Dapr pub/sub
   app.Run();
   ```

### Adding a New API Endpoint

1. **Define OpenAPI Contract**:
   - Update `specs/001-hr-system-simulator/contracts/<service>.openapi.yaml`

2. **Implement Controller**:
   ```csharp
   [ApiController]
   [Route("v1/[controller]")]
   public class EmployeesController : ControllerBase
   {
       private readonly DaprClient _daprClient;
       
       [HttpGet("{id}")]
       public async Task<ActionResult<Employee>> GetEmployee(string id)
       {
           var employee = await _daprClient.GetStateAsync<Employee>(
               "statestore", $"employee:{id}");
           
           if (employee == null)
               return NotFound();
           
           return Ok(employee);
       }
   }
   ```

3. **Add Consumer Contract Test** (Frontend):
   ```typescript
   // frontend/tests/employee.pact.test.ts
   describe('Employee API', () => {
     it('returns employee details', async () => {
       await provider.addInteraction({
         state: 'employee 123 exists',
         uponReceiving: 'a request for employee 123',
         withRequest: {
           method: 'GET',
           path: '/v1/employees/123',
         },
         willRespondWith: {
           status: 200,
           body: { id: '123', name: 'John Doe' },
         },
       });
     });
   });
   ```

4. **Add Provider Verification** (.NET):
   ```csharp
   [Fact]
   public void EmployeeService_HonorsContracts()
   {
       new PactVerifier()
           .ServiceProvider("EmployeeService", "http://localhost:5001")
           .PactUri("../pacts/frontend-employeeservice.json")
           .Verify();
   }
   ```

---

## Dapr Configuration

### Local Development Components

Dapr components for local dev are in `.dapr/components/`:

**State Store (Redis)**:
```yaml
# .dapr/components/statestore.yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: statestore
spec:
  type: state.redis
  version: v1
  metadata:
  - name: redisHost
    value: localhost:6379
  - name: redisPassword
    value: ""
```

**Pub/Sub (In-Memory)**:
```yaml
# .dapr/components/pubsub.yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: pubsub
spec:
  type: pubsub.in-memory
  version: v1
```

### Production Components (Azure)

For Azure deployment, Dapr components use:
- **State Store**: Azure Cosmos DB
- **Pub/Sub**: Azure Service Bus
- **Secrets**: Azure Key Vault

Components configured via Bicep in `infra/` directory.

---

## Common Tasks

### View Employee Data (Redis)

```bash
# Connect to Redis
redis-cli

# List all keys
KEYS *

# View employee data
GET employee:123

# View index
GET email-index:john@example.com
```

### Trigger Dapr Event Manually

```bash
# Publish event to Dapr
curl -X POST http://localhost:3500/v1.0/publish/pubsub/employee-created \
  -H "Content-Type: application/json" \
  -d '{"employeeId": "123", "name": "John Doe"}'
```

### View Dapr Logs

```bash
# View Dapr sidecar logs for a service
dapr logs --app-id employee-service
```

### Reset Local State

```bash
# Clear Redis data
redis-cli FLUSHALL

# Restart services
dotnet run --project src/AppHost
```

---

## Troubleshooting

### Issue: Dapr services not starting

**Solution**:
```bash
# Reinitialize Dapr
dapr uninstall
dapr init

# Verify Dapr is running
docker ps | grep dapr
```

### Issue: Port conflicts (e.g., 5001 already in use)

**Solution**:
```bash
# Find process using port
lsof -i :5001  # macOS/Linux
netstat -ano | findstr :5001  # Windows

# Kill process or change port in AppHost/Program.cs
```

### Issue: Frontend can't reach backend

**Solution**:
1. Check Aspire dashboard for service URLs
2. Verify CORS configuration in backend services:
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowFrontend", policy =>
           policy.WithOrigins("http://localhost:3000")
                 .AllowAnyMethod()
                 .AllowAnyHeader());
   });
   ```

### Issue: Contract tests failing

**Solution**:
1. Regenerate Pact files: `npm run test:contract` in frontend
2. Verify service is running: `curl http://localhost:5001/health`
3. Check Pact file path in provider verification

---

## Testing Strategy

### Unit Tests

```bash
# Run unit tests for a service
dotnet test tests/EmployeeService.Tests --filter Category=Unit

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### Contract Tests

```bash
# Generate consumer contracts (Frontend)
cd frontend && npm run test:contract

# Verify provider contracts (.NET)
dotnet test tests/Contract.Tests
```

### Integration Tests

```bash
# Run integration tests (uses TestContainers for Redis)
dotnet test tests/EmployeeService.Tests --filter Category=Integration
```

### End-to-End Tests

```bash
# Run E2E tests with Playwright
cd frontend && npm run test:e2e
```

---

## Next Steps

1. **Implement Business Logic**: Start with Employee Service CRUD operations
2. **Add Event Handlers**: Subscribe to Dapr pub/sub events in dependent services
3. **Build Frontend Pages**: Create React components for employee management
4. **Write Tests**: Add unit tests for controllers, contract tests for APIs
5. **Deploy to Azure**: Use Bicep templates in `infra/` for Azure Container Apps

---

## Additional Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Dapr Documentation](https://docs.dapr.io/)
- [OpenAPI/Swagger](https://swagger.io/specification/)
- [Pact Contract Testing](https://docs.pact.io/)
- [React Testing Library](https://testing-library.com/react)

---

## Support

For issues or questions:
1. Check Aspire Dashboard logs: http://localhost:15000
2. Review Dapr sidecar logs: `dapr logs --app-id <service-name>`
3. Consult [research.md](research.md) for architectural decisions
4. Refer to [data-model.md](data-model.md) for entity schemas
