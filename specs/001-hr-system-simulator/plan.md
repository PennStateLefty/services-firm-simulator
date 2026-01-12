# Implementation Plan: HR System Simulator

**Branch**: `001-hr-system-simulator` | **Date**: 2026-01-09 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-hr-system-simulator/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build an HR system simulator for professional services firms that handles employee onboarding, performance management, merit processing, and offboarding. The system will use .NET 10 for business logic and services, Dapr for interservice communication and state management, .NET Aspire for local observability, and a minimal React frontend calling RESTful APIs. Architecture follows Azure cloud-native patterns with testable, observable, and extensible design.

## Technical Context

**Language/Version**: .NET 10 (C#) for backend services, React 18+ (JavaScript/TypeScript) for frontend  
**Primary Dependencies**: Dapr (interservice communication, state management), .NET Aspire (observability, local runtime), ASP.NET Core (RESTful APIs), minimal React libraries (React Router for routing, native fetch for API calls)  
**Storage**: Dapr state store abstraction (Azure Cosmos DB for production, Redis/in-memory for local development)  
**Testing**: xUnit for .NET unit/integration tests, contract testing for API boundaries, React Testing Library for frontend  
**Target Platform**: Azure Container Apps (production), Docker containers (local development), Aspire dashboard for observability  
**Project Type**: Web application (frontend + backend microservices)  
**Performance Goals**: Support 50 concurrent users, <2 second API response time, 90% review completion within cycle  
**Constraints**: Minimal external dependencies for frontend, testable service boundaries, Azure-first design  
**Scale/Scope**: Target up to 500 employees, 5-7 microservices, annual performance cycles

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Quality ✅ PASS

- **Testability**: All services will use dependency injection, Dapr components are abstracted for testing, xUnit and contract testing required
- **Contract Testing**: RESTful API contracts between frontend and backend, Dapr pub/sub message contracts between services
- **Linters**: ESLint for React frontend, Roslyn analyzers for .NET backend, enforced in CI/CD
- **Coverage Targets**: 70% line coverage for business logic, 90% for service boundaries

### II. Maintainability ✅ PASS

- **Simplicity**: Using Dapr state store abstraction instead of complex ORM, minimal React libraries, direct service-to-service patterns
- **Simulator-Appropriate**: In-memory/Redis state for local dev, avoiding production-scale complexity like CQRS or event sourcing
- **3-Layer Maximum**: Presentation (React), Business Logic (.NET services), Data Access (Dapr state store)
- **Justification**: No unnecessary abstractions, focusing on demonstration value

### III. Observability ✅ PASS

- **Structured Logging**: JSON logs with correlation IDs via .NET logging and Aspire
- **Tracing**: OpenTelemetry integration via Aspire for distributed tracing across Dapr services
- **Azure Integration**: Application Insights for production, Aspire dashboard for local development
- **Health Checks**: ASP.NET Core health endpoints for all services
- **Observable State**: Performance review status, merit processing, onboarding progress visible in logs and dashboard

### IV. Extensibility ✅ PASS

- **Modular Services**: Separate services for Employee, Onboarding, Performance, Merit, Offboarding
- **Configuration-Driven**: Merit guidelines, onboarding templates configurable via appsettings/environment
- **Clear Boundaries**: Dapr service invocation and pub/sub provide extension points for new scenarios
- **Scenario Addition**: New HR processes can be added as new services without modifying core logic

### V. Cloud-Native ✅ PASS

- **Azure Services**: Dapr components map to Azure Service Bus (pub/sub), Cosmos DB (state), Container Apps (hosting)
- **Containerization**: All .NET services and React frontend containerized, orchestrated via Aspire locally
- **Stateless Services**: All state externalized to Dapr state stores
- **Configuration**: Environment variables and Azure App Configuration for settings
- **IaC**: Bicep templates for Azure resource provisioning
- **Local Development**: Aspire provides local development experience without live Azure resources

**GATE DECISION**: ✅ ALL GATES PASS - Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/001-hr-system-simulator/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── README.md
│   ├── employee-service.openapi.yaml
│   └── performance-service.openapi.yaml
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
# Web application (frontend + backend microservices)
src/
├── AppHost/
│   └── Program.cs                # Aspire orchestration configuration
├── EmployeeService/
│   ├── Controllers/
│   ├── Models/
│   ├── Services/
│   └── Program.cs
├── OnboardingService/
│   ├── Controllers/
│   ├── Models/
│   ├── Services/
│   └── Program.cs
├── PerformanceService/
│   ├── Controllers/
│   ├── Models/
│   ├── Services/
│   └── Program.cs
├── MeritService/
│   ├── Controllers/
│   ├── Models/
│   ├── Services/
│   └── Program.cs
└── OffboardingService/
    ├── Controllers/
    ├── Models/
    ├── Services/
    └── Program.cs

frontend/
├── src/
│   ├── api/                      # API client logic
│   ├── components/               # Reusable React components
│   ├── pages/                    # Route pages
│   ├── hooks/                    # Custom React hooks
│   └── App.tsx
├── tests/
│   └── contract/                 # Pact consumer tests
└── package.json

tests/
├── EmployeeService.Tests/
├── OnboardingService.Tests/
├── PerformanceService.Tests/
├── MeritService.Tests/
├── OffboardingService.Tests/
└── Contract.Tests/               # Pact provider tests

infra/
├── main.bicep                    # Azure infrastructure
├── containerApps.bicep
└── dapr-components/
    ├── statestore.yaml
    └── pubsub.yaml

.dapr/
└── components/                   # Local Dapr components
    ├── statestore.yaml
    └── pubsub.yaml
```

**Structure Decision**: Web application structure selected due to frontend + backend architecture. Five .NET microservices for core HR domains (Employee, Onboarding, Performance, Merit, Offboarding), React frontend as separate application. Aspire AppHost orchestrates all services locally. Dapr components configured for both local (Redis/in-memory) and production (Cosmos DB/Service Bus) environments.

## Complexity Tracking

**Status**: No violations detected. All design choices align with constitution principles.

### Constitution Re-evaluation Post-Design

After completing Phase 1 design (data model, contracts, project structure):

**I. Quality** ✅ CONFIRMED
- OpenAPI contracts defined for Employee and Performance services
- Pact.NET contract testing strategy documented
- xUnit for unit tests, React Testing Library for frontend
- All services use dependency injection for testability

**II. Maintainability** ✅ CONFIRMED
- 5 microservices (not excessive, clear domain boundaries)
- Simple Dapr state store pattern (no complex ORM)
- 3-layer architecture: API Controllers → Business Logic → Dapr State Store
- Minimal React dependencies (Router + native fetch only)

**III. Observability** ✅ CONFIRMED
- .NET Aspire provides unified observability dashboard
- OpenTelemetry integration for distributed tracing
- Structured logging via .NET logging extensions
- Health endpoints on all services

**IV. Extensibility** ✅ CONFIRMED
- New HR processes can be added as new services
- Dapr pub/sub events enable loose coupling
- Merit guidelines configurable via appsettings
- Clear service boundaries prevent tight coupling

**V. Cloud-Native** ✅ CONFIRMED
- All services containerized for Azure Container Apps
- Dapr components map to Azure services (Cosmos DB, Service Bus)
- Bicep templates for infrastructure as code
- Aspire provides local development without live Azure

**FINAL GATE DECISION**: ✅ ALL PRINCIPLES SATISFIED - Proceed to Phase 2 (Task Planning)
