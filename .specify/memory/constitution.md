<!--
╔══════════════════════════════════════════════════════════════════════════════╗
║                        CONSTITUTION SYNC IMPACT REPORT                       ║
╚══════════════════════════════════════════════════════════════════════════════╝

VERSION CHANGE: [NEW] → 1.0.0 (Initial constitution ratification)

PRINCIPLES ESTABLISHED:
  + I. Quality - Testability and contract verification
  + II. Maintainability - Simplicity over production complexity
  + III. Observability - Transparent simulation behavior
  + IV. Extensibility - Modular scenario design
  + V. Cloud-Native - Azure and cloud-first architecture

SECTIONS ADDED:
  + Core Principles (5 principles)
  + Development Workflow (quality gates and review process)
  + Governance (amendment procedures and compliance)

TEMPLATES REQUIRING UPDATES:
  ✅ plan-template.md - Constitution Check section aligns with principles
  ✅ spec-template.md - Requirements structure supports testability
  ✅ tasks-template.md - Task phases align with test-first and independent testing
  ⚠ Agent/command files - May reference generic patterns vs Azure-specific

FOLLOW-UP ACTIONS:
  • Review agent files for Azure-specific guidance alignment
  • Consider adding quickstart examples for observability patterns
  • Document contract testing patterns in development guidelines
  • Add linter configuration examples for chosen languages

DATE: 2026-01-09
-->

# Services Firm Simulator Constitution

## Core Principles

### I. Quality

**All code MUST be testable; All contracts MUST leverage contract testing; Linters MUST be used where available.**

**Rationale**: As a demonstration tool for Azure practitioners in technical sales, the simulator must exhibit professional quality standards. Testable code enables confident modifications, contract testing ensures API/message reliability across service boundaries, and linters catch common errors early. This principle ensures the simulator remains a credible example of best practices.

**Implementation Requirements**:
- Every function/method must be independently testable (pure functions preferred, dependency injection required)
- API contracts, message schemas, and service interfaces must have automated contract tests (e.g., Pact, Spring Cloud Contract, or equivalent)
- Language-appropriate linters must be configured and enforced in CI/CD (e.g., ESLint for TypeScript, Pylint/Ruff for Python, golangci-lint for Go)
- Code coverage targets: minimum 70% line coverage for business logic, 90% for contract interfaces
- Tests must be runnable locally without external dependencies (use mocks, stubs, or containers)

### II. Maintainability

**Code MUST be simple; Production-grade complexity MUST be avoided; Simulator-appropriate patterns MUST be favored.**

**Rationale**: This is a simulator designed for demonstration purposes, not a production system. Overengineering reduces maintainability and obscures the demonstration value. Simple, readable code enables rapid scenario modifications and helps sales engineers understand and explain the system architecture to customers.

**Implementation Requirements**:
- Prefer direct implementations over abstraction layers unless abstraction serves demonstration value
- Avoid enterprise patterns (e.g., extensive repository layers, complex ORMs) unless demonstrating those patterns is the point
- Use in-memory data stores or simple persistence (e.g., SQLite, JSON files) unless demonstrating cloud-native storage
- Limit architectural layers: presentation, business logic, data access (3 layers maximum)
- Documentation must explain "why" for any non-obvious design choice
- Code reviews must challenge unnecessary complexity

### III. Observability

**System behavior MUST be easily observable; Structured logging MUST be implemented; Tracing MUST support demonstration scenarios.**

**Rationale**: As a simulation tool, the system's primary value is showing how services interact and behave under various scenarios. Observability is not a secondary concern but a core feature that enables effective demonstrations and troubleshooting during sales engagements.

**Implementation Requirements**:
- Structured logging in JSON format with correlation IDs (trace ID, span ID, request ID)
- All significant operations must emit logs with context (who, what, when, why, result)
- Integration with Azure Application Insights or equivalent observability platform
- Distributed tracing support (e.g., OpenTelemetry) across service boundaries
- Health checks and readiness probes for all services
- Observable simulation state changes (e.g., order status transitions, employee assignments)
- Dashboard or console output showing real-time simulation activity

### IV. Extensibility

**New scenarios MUST be easy to add; Business logic MUST be modular; Scenario configuration MUST be declarative where possible.**

**Rationale**: The simulator's value increases with the variety of scenarios it can demonstrate. Extensibility ensures that new use cases (e.g., different industry verticals, failure modes, scaling patterns) can be added without architectural rewrites, maximizing ROI on the codebase.

**Implementation Requirements**:
- Scenario definitions must be configuration-driven (YAML, JSON, or code-as-config)
- Business rules must be pluggable (strategy pattern, event handlers, or equivalent)
- Clear extension points documented (interfaces, base classes, event subscriptions)
- New scenarios should require adding configuration + minimal code, not modifying core logic
- Avoid tight coupling between scenarios (one scenario's logic should not depend on another)
- Version scenarios independently when possible

### V. Cloud-Native

**Library, protocol, and design choices MUST support cloud-native runtime; Azure services MUST be preferred; Containerization and orchestration MUST be first-class concerns.**

**Rationale**: The simulator demonstrates Azure capabilities for services industry businesses. Design choices must align with Azure's ecosystem and cloud-native principles (scalability, resilience, observability) to authentically represent modern cloud architecture patterns.

**Implementation Requirements**:
- Services must run in containers (Docker) and be orchestratable (Kubernetes, Azure Container Apps, or AKS)
- Use Azure-native services where appropriate: Azure Service Bus, Azure Cosmos DB, Azure Functions, Azure API Management, Azure Monitor
- Stateless services preferred; state externalized to Azure storage services
- Configuration via environment variables or Azure App Configuration
- Authentication via Azure AD / Entra ID or managed identities
- APIs must follow REST or gRPC conventions suitable for cloud gateways
- Infrastructure as Code (Bicep, Terraform, or ARM templates) for Azure resource provisioning
- Support for local development (Docker Compose, Azurite) without requiring live Azure resources

## Development Workflow

**Quality Gates** (MUST pass before merging):
1. All linters pass with zero errors
2. Contract tests pass (if contracts modified)
3. Unit/integration tests pass with minimum coverage thresholds
4. Observability logging validated (structured logs present for key operations)
5. Code review approved by at least one maintainer

**Review Process**:
- Reviewers must verify adherence to all five core principles
- Complexity violations must be explicitly justified in PR description
- New scenarios must include configuration examples and usage documentation
- Any new Azure service dependency must be documented with local development alternative

**Test-First Discipline** (RECOMMENDED but not enforced):
- Write tests for contracts and critical business logic before implementation
- Ensure tests fail before implementation (Red-Green-Refactor)
- Tests must be independently runnable (no sequential dependencies)

## Governance

**Authority**: This constitution supersedes all other development practices. In case of conflict, principles defined here take precedence.

**Amendment Procedure**:
- Amendments require proposal with rationale and impact analysis
- Proposals must identify affected templates, code, and documentation
- Approval requires consensus from project maintainers
- Amendment commits must increment constitution version appropriately
- All dependent artifacts (templates, command files, guidelines) must be updated in the same change or tracked as follow-up

**Versioning Policy**:
- MAJOR: Principle removed, redefined, or made more restrictive (breaking)
- MINOR: New principle added or existing principle expanded (additive)
- PATCH: Clarifications, typo fixes, wording improvements (non-semantic)

**Compliance**:
- All feature specifications must verify compliance with core principles
- Plans must include "Constitution Check" section mapping requirements to principles
- Non-compliance must be explicitly justified and tracked in "Complexity Tracking" section
- Periodic audits (quarterly recommended) to ensure codebase alignment

**Guidance Artifact**: Runtime development guidance is maintained in `.specify/templates/agent-file-template.md` and auto-generated from feature plans.

**Version**: 1.0.0 | **Ratified**: 2026-01-09 | **Last Amended**: 2026-01-09
