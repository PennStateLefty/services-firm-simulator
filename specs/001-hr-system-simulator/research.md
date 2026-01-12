# Research: HR System Simulator

**Date**: 2026-01-09  
**Status**: Phase 0 Complete

## Overview

This document captures research findings for implementing an HR system simulator using .NET 10, Dapr, .NET Aspire, and React. The goal is to identify best practices, architectural patterns, and implementation strategies that align with the constitution's principles of quality, maintainability, observability, extensibility, and cloud-native design.

---

## 1. .NET 10 + Dapr Microservices Architecture

### Decision: Service-per-Aggregate Pattern with Dapr Sidecar

**Rationale**: 
- Each HR domain aggregate (Employee, Onboarding, Performance, Merit, Offboarding) becomes an independent .NET service
- Dapr sidecars handle cross-cutting concerns (service discovery, state management, pub/sub) without coupling services
- Aligns with maintainability (simple service boundaries) and extensibility (new services added independently)

**Architecture Pattern**:
```
Employee Service (.NET 10 + Dapr)
├── API Controllers (REST endpoints)
├── Business Logic (domain models, validation)
├── Dapr State Store (employee data persistence)
└── Dapr Pub/Sub (emit employee lifecycle events)

Performance Service (.NET 10 + Dapr)
├── API Controllers (REST endpoints)
├── Business Logic (review management, rating calculations)
├── Dapr State Store (review data persistence)
├── Dapr Service Invocation (call Employee Service for data)
└── Dapr Pub/Sub (subscribe to employee events, emit review events)
```

**Best Practices**:
- Use `Dapr.AspNetCore` SDK for .NET integration
- Configure Dapr components (statestore, pubsub) via YAML for environment portability
- Implement retry policies and circuit breakers via Dapr resiliency specs
- Use Dapr service invocation for synchronous queries, pub/sub for async workflows
- Version API endpoints (`/v1/employees`) for future contract evolution

**Alternatives Considered**:
- Monolithic .NET application: Rejected due to lack of extensibility for new scenarios
- Event sourcing with CQRS: Rejected as overengineering for simulator (violates Principle II)
- Direct HTTP calls between services: Rejected in favor of Dapr service invocation for resilience and observability

---

## 2. .NET Aspire for Local Development and Observability

### Decision: Aspire AppHost for Orchestration and Dashboard for Observability

**Rationale**:
- .NET Aspire provides local development orchestration without Docker Compose complexity
- Aspire dashboard surfaces OpenTelemetry traces, logs, and metrics in unified UI
- Aligns with observability (Principle III) and cloud-native (Principle V) goals

**Implementation Pattern**:
```csharp
// AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis"); // Dapr state store
var serviceBus = builder.AddAzureServiceBus("messaging"); // Dapr pub/sub

var employeeService = builder.AddProject<Projects.EmployeeService>("employee-service")
    .WithDaprSidecar()
    .WithReference(redis);

var performanceService = builder.AddProject<Projects.PerformanceService>("performance-service")
    .WithDaprSidecar()
    .WithReference(redis)
    .WithReference(serviceBus);

var frontend = builder.AddNpmApp("frontend", "../frontend")
    .WithReference(employeeService)
    .WithReference(performanceService);

builder.Build().Run();
```

**Best Practices**:
- Use Aspire service discovery for frontend-to-backend API calls
- Configure Aspire to emit OpenTelemetry to Application Insights in production
- Use Aspire health checks to monitor Dapr sidecar readiness
- Leverage Aspire secrets management for local development (user secrets) and Azure Key Vault in production

**Alternatives Considered**:
- Docker Compose: Rejected in favor of Aspire's native .NET integration and superior observability
- Tye (predecessor): Rejected as Aspire is the official Microsoft solution
- Manual service startup: Rejected due to poor developer experience and lack of observability

---

## 3. Dapr State Store Patterns for HR Data

### Decision: Document-per-Entity with Composite Keys

**Rationale**:
- HR entities (Employee, Review, Merit Proposal) are naturally document-oriented with embedded relationships
- Dapr state store abstraction allows switching between Redis (local), Cosmos DB (production) without code changes
- Composite keys (e.g., `employee:{id}`, `review:{cycleId}:{employeeId}`) enable efficient querying
- Aligns with simplicity (no ORM complexity) and cloud-native (Azure Cosmos DB ready)

**Data Access Pattern**:
```csharp
// Employee Service
public class EmployeeService
{
    private readonly DaprClient _daprClient;

    public async Task<Employee> GetEmployeeAsync(string employeeId)
    {
        return await _daprClient.GetStateAsync<Employee>(
            "statestore", 
            $"employee:{employeeId}"
        );
    }

    public async Task SaveEmployeeAsync(Employee employee)
    {
        await _daprClient.SaveStateAsync(
            "statestore", 
            $"employee:{employee.Id}", 
            employee,
            metadata: new Dictionary<string, string> 
            { 
                { "contentType", "application/json" } 
            }
        );
    }
}
```

**Querying Strategy**:
- Use state store metadata for simple queries (e.g., status="Active")
- For complex queries (e.g., all reviews in cycle), maintain index documents (e.g., `review-index:{cycleId}` → list of employee IDs)
- Accept eventual consistency for cross-service queries (e.g., Performance Service caching employee names)

**Alternatives Considered**:
- Entity Framework Core with SQL: Rejected due to ORM complexity and limited Azure Cosmos DB support
- Direct Azure SDK calls: Rejected to maintain Dapr abstraction for testability
- CQRS with separate read models: Rejected as overengineering for simulator scale

---

## 4. Contract Testing for .NET RESTful APIs

### Decision: Use Pact.NET for Consumer-Driven Contract Testing

**Rationale**:
- Pact enables frontend (consumer) to define expected API contracts, backend (provider) verifies compliance
- Catches breaking changes before deployment, aligns with quality (Principle I)
- Works well with microservices where each service has distinct API contracts

**Implementation Pattern**:
```csharp
// Frontend (Consumer) Test
[Fact]
public async Task GetEmployee_ReturnsEmployeeDetails()
{
    var pact = Pact.V3("Frontend", "EmployeeService", new PactConfig());
    
    pact.UponReceiving("A request for employee details")
        .Given("Employee 123 exists")
        .WithRequest(HttpMethod.Get, "/v1/employees/123")
        .WillRespond()
        .WithStatus(200)
        .WithJsonBody(new
        {
            id = "123",
            name = "John Doe",
            department = "Engineering"
        });

    await pact.VerifyAsync(async ctx =>
    {
        var client = new HttpClient { BaseAddress = ctx.MockServerUri };
        var response = await client.GetAsync("/v1/employees/123");
        // Assert response matches expected contract
    });
}

// Backend (Provider) Verification
[Fact]
public void VerifyEmployeeServiceContracts()
{
    var config = new PactVerifierConfig();
    
    new PactVerifier(config)
        .ServiceProvider("EmployeeService", "http://localhost:5000")
        .WithProviderStateUrl("http://localhost:5000/provider-states")
        .PactUri("../pacts/frontend-employeeservice.json")
        .Verify();
}
```

**Best Practices**:
- Store Pact files in shared repository or Pact Broker
- Run provider verification in CI/CD before deployment
- Use provider states to set up test data (e.g., "Employee 123 exists")
- Version contracts alongside API versions

**Alternatives Considered**:
- OpenAPI schema validation: Rejected as it doesn't test actual consumer-provider interaction
- Integration tests only: Rejected as they don't catch contract drift early
- Manual API testing: Rejected due to lack of automation and regression safety

---

## 5. Minimal React Architecture for RESTful API Consumption

### Decision: React + React Router + Native Fetch with Custom Hooks

**Rationale**:
- Minimal dependencies align with maintainability (Principle II)
- React Router for navigation, native fetch for API calls (no axios or React Query overhead)
- Custom hooks encapsulate API logic, making components testable
- TypeScript for type safety on API contracts

**Architecture Pattern**:
```
frontend/
├── src/
│   ├── api/
│   │   ├── apiClient.ts         # Base fetch wrapper with error handling
│   │   ├── employeeApi.ts       # Employee service endpoints
│   │   └── performanceApi.ts    # Performance service endpoints
│   ├── hooks/
│   │   ├── useEmployee.ts       # Custom hook for employee data
│   │   └── usePerformance.ts    # Custom hook for performance data
│   ├── components/
│   │   ├── EmployeeList.tsx
│   │   └── PerformanceReview.tsx
│   ├── pages/
│   │   ├── Dashboard.tsx
│   │   └── EmployeeDetail.tsx
│   └── App.tsx                  # React Router setup
└── tests/
    ├── components/              # React Testing Library tests
    └── api/                     # Pact consumer tests
```

**API Client Pattern**:
```typescript
// api/apiClient.ts
export async function apiCall<T>(
  url: string, 
  options?: RequestInit
): Promise<T> {
  const response = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  });

  if (!response.ok) {
    throw new Error(`API error: ${response.status}`);
  }

  return response.json();
}

// hooks/useEmployee.ts
export function useEmployee(employeeId: string) {
  const [employee, setEmployee] = useState<Employee | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    apiCall<Employee>(`/v1/employees/${employeeId}`)
      .then(setEmployee)
      .catch(setError)
      .finally(() => setLoading(false));
  }, [employeeId]);

  return { employee, loading, error };
}
```

**Best Practices**:
- Use TypeScript interfaces for API response types (generated from OpenAPI or manually maintained)
- Implement error boundaries for API error handling
- Use React Testing Library for component tests with mocked API responses
- Keep API logic in custom hooks, not in components

**Alternatives Considered**:
- React Query: Rejected as overkill for simulator (adds caching complexity)
- Redux: Rejected due to boilerplate overhead for simple CRUD operations
- Axios: Rejected as native fetch is sufficient with modern browser support
- SWR: Rejected to minimize dependencies (same rationale as React Query)

---

## 6. Service Decomposition Strategy

### Decision: 5 Core Services Aligned with HR Domains

**Rationale**:
- Each service maps to a key entity from the feature specification
- Clear bounded contexts prevent service coupling
- Aligns with extensibility (new services for new HR processes) and maintainability (simple service boundaries)

**Service Boundaries**:

1. **Employee Service**: Manages employee records (CRUD, search, directory)
   - State: Employee documents
   - Events: EmployeeCreated, EmployeeUpdated, EmployeeTerminated
   - APIs: GET/POST/PUT /v1/employees, GET /v1/employees/search

2. **Onboarding Service**: Manages new hire onboarding workflows
   - State: OnboardingCase documents, Task documents
   - Events: OnboardingStarted, TaskCompleted, OnboardingCompleted
   - Dependencies: Subscribes to EmployeeCreated, calls Employee Service for data
   - APIs: POST /v1/onboarding, GET /v1/onboarding/{caseId}, PUT /v1/onboarding/tasks/{taskId}

3. **Performance Service**: Manages performance reviews and cycles
   - State: ReviewCycle documents, PerformanceReview documents
   - Events: ReviewCycleStarted, ReviewSubmitted, ReviewCycleCompleted
   - Dependencies: Subscribes to EmployeeTerminated (archive reviews), calls Employee Service
   - APIs: POST /v1/performance/cycles, POST /v1/performance/reviews, GET /v1/performance/reviews/{employeeId}

4. **Merit Service**: Processes compensation adjustments
   - State: MeritCycle documents, MeritProposal documents
   - Events: MeritCycleStarted, MeritApplied
   - Dependencies: Subscribes to ReviewCycleCompleted, calls Employee Service and Performance Service
   - APIs: POST /v1/merit/cycles, GET /v1/merit/proposals, POST /v1/merit/apply

5. **Offboarding Service**: Manages employee departures
   - State: OffboardingCase documents, Task documents
   - Events: OffboardingStarted, OffboardingCompleted
   - Dependencies: Publishes EmployeeTerminated (consumed by Employee Service), calls Employee Service
   - APIs: POST /v1/offboarding, GET /v1/offboarding/{caseId}, PUT /v1/offboarding/tasks/{taskId}

**Cross-Cutting Concerns**:
- Authentication/Authorization: Future enhancement (currently simulator allows open access)
- Audit Logging: Handled via Aspire/OpenTelemetry structured logs
- Configuration: Shared via Azure App Configuration or environment variables

**Alternatives Considered**:
- Single monolithic service: Rejected due to lack of extensibility for new scenarios
- 10+ microservices: Rejected as overengineering (violates Principle II)
- Event-sourced aggregates: Rejected due to complexity for simulator use case

---

## 7. Testing Strategy

### Decision: 3-Tier Testing Pyramid with Contract Tests

**Rationale**:
- Unit tests for business logic (fast, isolated)
- Contract tests for API boundaries (catch integration issues)
- End-to-end tests for critical user flows (verify full stack)
- Aligns with quality (Principle I) and testability requirements

**Test Coverage Targets**:
- Unit tests: 70% line coverage for business logic (domain models, calculations)
- Contract tests: 90% coverage for API endpoints and pub/sub messages
- E2E tests: Cover 5 primary user stories (onboarding, performance, merit, offboarding, directory)

**Test Implementations**:

1. **Unit Tests (xUnit + Moq)**:
   ```csharp
   public class MeritCalculatorTests
   {
       [Theory]
       [InlineData(5, 5.0)] // Rating 5 → 5% raise
       [InlineData(4, 3.0)] // Rating 4 → 3% raise
       [InlineData(3, 2.0)] // Rating 3 → 2% raise
       public void CalculateRaise_WithPerformanceRating_ReturnsCorrectPercentage(
           int rating, decimal expectedPercentage)
       {
           var calculator = new MeritCalculator();
           var result = calculator.CalculateRaisePercentage(rating);
           Assert.Equal(expectedPercentage, result);
       }
   }
   ```

2. **Contract Tests (Pact.NET)**:
   - Consumer tests: React frontend defines expected API responses
   - Provider tests: .NET services verify compliance with contracts

3. **Integration Tests (WebApplicationFactory)**:
   ```csharp
   public class EmployeeServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
   {
       [Fact]
       public async Task CreateEmployee_ReturnsCreatedEmployee()
       {
           var client = _factory.CreateClient();
           var employee = new { name = "Test User", email = "test@example.com" };
           
           var response = await client.PostAsJsonAsync("/v1/employees", employee);
           
           response.EnsureSuccessStatusCode();
           var result = await response.Content.ReadFromJsonAsync<Employee>();
           Assert.NotNull(result.Id);
       }
   }
   ```

4. **E2E Tests (Playwright)**:
   - Simulate full user workflows (onboard employee → submit review → process merit)
   - Run against Aspire-orchestrated environment

**Best Practices**:
- Use TestContainers for integration tests requiring Redis/Cosmos DB
- Mock Dapr components in unit tests (DaprClient interface)
- Run contract tests in CI/CD before deployment
- E2E tests run nightly or before releases (slower, more brittle)

**Alternatives Considered**:
- Only integration tests: Rejected due to slow feedback and high maintenance
- Manual testing: Rejected due to lack of regression safety
- Snapshot testing for APIs: Rejected as less explicit than contract tests

---

## 8. Local Development Experience

### Decision: Aspire AppHost with Hot Reload and Aspire Dashboard

**Rationale**:
- Single command (`dotnet run --project AppHost`) starts all services, Dapr sidecars, and dependencies
- Aspire dashboard provides observability without external tools
- Hot reload for .NET services and React frontend accelerates development
- Aligns with cloud-native (Principle V) and observability (Principle III)

**Developer Workflow**:
1. Clone repository
2. Run `dotnet run --project src/AppHost` (starts all services via Aspire)
3. Open `http://localhost:15000` (Aspire dashboard) for observability
4. Open `http://localhost:3000` (React frontend) to use application
5. Make code changes → automatic hot reload

**Configuration**:
- Aspire uses .NET user secrets for local development (no hardcoded credentials)
- Dapr components configured for Redis (local) and in-memory pub/sub
- Production uses Azure Cosmos DB (state) and Azure Service Bus (pub/sub) via Dapr component swap

**Alternatives Considered**:
- Docker Compose: Rejected in favor of Aspire's native .NET integration
- Kubernetes locally (Minikube): Rejected as overengineering for local dev
- Separate terminal windows for each service: Rejected due to poor developer experience

---

## 9. Deployment and Infrastructure

### Decision: Azure Container Apps with Bicep IaC

**Rationale**:
- Azure Container Apps provides managed Dapr runtime (no manual sidecar management)
- Bicep for infrastructure as code (Azure-native, simpler than Terraform for Azure-only)
- Aligns with cloud-native (Principle V) and Azure-first architecture

**Deployment Architecture**:
```
Azure Container Apps Environment
├── Employee Service (Container App + Dapr)
├── Onboarding Service (Container App + Dapr)
├── Performance Service (Container App + Dapr)
├── Merit Service (Container App + Dapr)
├── Offboarding Service (Container App + Dapr)
└── Frontend (Static Web App or Container App)

Shared Resources:
├── Azure Cosmos DB (Dapr state store)
├── Azure Service Bus (Dapr pub/sub)
├── Azure Application Insights (observability)
├── Azure Container Registry (container images)
└── Azure Key Vault (secrets)
```

**Bicep Template Pattern**:
```bicep
resource containerAppsEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: 'hr-simulator-env'
  location: location
  properties: {
    daprAIConnectionString: applicationInsights.properties.ConnectionString
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: { customerId: logAnalytics.properties.customerId }
    }
  }
}

resource employeeService 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'employee-service'
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnv.id
    configuration: {
      dapr: {
        enabled: true
        appId: 'employee-service'
        appPort: 80
      }
      ingress: {
        external: true
        targetPort: 80
      }
    }
    template: {
      containers: [{
        name: 'employee-service'
        image: '${containerRegistry.properties.loginServer}/employee-service:latest'
      }]
    }
  }
}
```

**CI/CD Pipeline**:
1. Build .NET services and React frontend (GitHub Actions or Azure DevOps)
2. Run unit and contract tests
3. Build container images, push to Azure Container Registry
4. Deploy Bicep template (update Container Apps with new images)
5. Run smoke tests against production

**Alternatives Considered**:
- Azure Kubernetes Service (AKS): Rejected due to operational complexity for simulator
- Azure App Service: Rejected as it doesn't support Dapr natively
- Terraform: Rejected in favor of Bicep for Azure-native IaC

---

## Summary of Key Decisions

| Area | Decision | Primary Rationale |
|------|----------|-------------------|
| Architecture | 5 microservices (Employee, Onboarding, Performance, Merit, Offboarding) | Clear domain boundaries, extensibility |
| Backend | .NET 10 + Dapr + ASP.NET Core | Cloud-native, testable, Azure-first |
| State Management | Dapr state store (Redis local, Cosmos DB production) | Simplicity, portability |
| Messaging | Dapr pub/sub (in-memory local, Service Bus production) | Decoupling, observability |
| Local Dev | .NET Aspire AppHost + Dashboard | Developer experience, observability |
| Frontend | React + React Router + Native Fetch | Minimal dependencies, testable |
| API Contracts | RESTful with Pact.NET contract testing | Quality, catch breaking changes |
| Testing | Unit (xUnit) + Contract (Pact) + E2E (Playwright) | Coverage at all levels |
| Deployment | Azure Container Apps + Bicep | Managed Dapr, Azure-native |
| Observability | OpenTelemetry + Aspire Dashboard + App Insights | Unified tracing, logging, metrics |

---

## Next Steps (Phase 1)

1. Generate data model (entities and relationships)
2. Define API contracts (OpenAPI specs for each service)
3. Create quickstart guide for local development setup
4. Update agent context with technology choices

**Research Complete**: All technical unknowns resolved. Ready for Phase 1 design.
