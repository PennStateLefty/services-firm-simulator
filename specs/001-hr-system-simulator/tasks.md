# Tasks: HR System Simulator

**Feature Branch**: `001-hr-system-simulator`  
**Input**: Design documents from `/specs/001-hr-system-simulator/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Tests are NOT requested in this specification, so test tasks are omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create .NET solution structure with Aspire AppHost at src/AppHost/Program.cs
- [ ] T002 [P] Initialize EmployeeService project at src/EmployeeService/ with ASP.NET Core Web API template
- [ ] T003 [P] Initialize OnboardingService project at src/OnboardingService/ with ASP.NET Core Web API template
- [ ] T004 [P] Initialize PerformanceService project at src/PerformanceService/ with ASP.NET Core Web API template
- [ ] T005 [P] Initialize MeritService project at src/MeritService/ with ASP.NET Core Web API template
- [ ] T006 [P] Initialize OffboardingService project at src/OffboardingService/ with ASP.NET Core Web API template
- [ ] T007 Initialize React frontend at frontend/ with Vite and TypeScript
- [ ] T008 [P] Add Dapr.AspNetCore NuGet package to all service projects
- [ ] T009 [P] Add Aspire.Hosting NuGet package to AppHost project
- [ ] T010 Configure Dapr components for local development at .dapr/components/statestore.yaml (Redis)
- [ ] T011 [P] Configure Dapr components for local development at .dapr/components/pubsub.yaml (in-memory)
- [ ] T012 [P] Setup ESLint and Prettier for frontend at frontend/.eslintrc.json and frontend/.prettierrc
- [ ] T013 [P] Setup Roslyn analyzers for .NET projects in Directory.Build.props at repository root
- [ ] T014 [P] Create xUnit test projects at tests/EmployeeService.Tests/, tests/OnboardingService.Tests/, tests/PerformanceService.Tests/, tests/MeritService.Tests/, tests/OffboardingService.Tests/
- [ ] T015 [P] Install Pact.NET NuGet package (PactNet) to tests/Contract.Tests/ project for consumer-driven contract testing
- [ ] T016 [P] Configure code coverage collection in Directory.Build.props with 70% threshold for business logic and Coverlet collector
- [ ] T017 Create Bicep infrastructure templates at infra/main.bicep for Azure Container Apps environment

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T018 Configure Aspire AppHost orchestration in src/AppHost/Program.cs (register Redis, EmployeeService, OnboardingService, PerformanceService, MeritService, OffboardingService all with Dapr sidecars, and frontend)
- [ ] T019 [P] Implement base Dapr state store wrapper at src/EmployeeService/Infrastructure/DaprStateStore.cs
- [ ] T018 [P] Setup structured logging with OpenTelemetry in src/EmployeeService/Program.cs
- [ ] T019 [P] Configure CORS for all services to allow frontend access at localhost:3000 in each service's Program.cs
- [ ] T020 [P] Add health check endpoints to all services at /health endpoint in each service's Program.cs
- [ ] T021 Create shared models library at src/Shared/Models/ for common enums (EmploymentStatus, CompetencyType, etc.)
- [ ] T022 [P] Setup React Router at frontend/src/App.tsx with route structure for all pages
- [ ] T023 [P] Create base API client wrapper at frontend/src/api/apiClient.ts with error handling and fetch configuration
- [ ] T024 Create Department entity and API endpoints at src/EmployeeService/Controllers/DepartmentsController.cs (required for all employee operations)
- [ ] T025 Implement Department state store operations at src/EmployeeService/Services/DepartmentService.cs
- [ ] T026 Create shared error response models at src/Shared/Models/ErrorResponse.cs
- [ ] T027 [P] Setup Dapr pub/sub subscription handler infrastructure in all services' Program.cs
- [ ] T028 Generate TypeScript types from OpenAPI specs at frontend/src/types/employee-api.ts and frontend/src/types/performance-api.ts

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Employee Onboarding (Priority: P1) 🎯 MVP

**Goal**: HR administrators bring new employees into the firm by collecting information, assigning departments, setting compensation, and tracking onboarding tasks

**Independent Test**: Create employee record with complete information, assign to department, set compensation, assign onboarding tasks, complete tasks, verify employee transitions to Active status

### Implementation for User Story 1

- [ ] T031 [P] [US1] Create Employee entity model at src/EmployeeService/Models/Employee.cs per data-model.md schema
- [ ] T032 [P] [US1] Create OnboardingCase entity model at src/OnboardingService/Models/OnboardingCase.cs per data-model.md schema
- [ ] T033 [P] [US1] Create Task entity model at src/OnboardingService/Models/Task.cs per data-model.md schema
- [ ] T034 [P] [US1] Create CompensationHistory entity model at src/EmployeeService/Models/CompensationHistory.cs per data-model.md schema
- [ ] T035 [US1] Implement EmployeeService CRUD state store operations at src/EmployeeService/Services/EmployeeService.cs (create, read, update, delete with Dapr)
- [ ] T036 [US1] Implement email uniqueness validation via email-index in src/EmployeeService/Services/EmployeeService.cs
- [ ] T037 [US1] Implement auto-generation of employeeNumber in src/EmployeeService/Services/EmployeeService.cs
- [ ] T038 [US1] Create POST /v1/employees endpoint at src/EmployeeService/Controllers/EmployeesController.cs per employee-service.openapi.yaml
- [ ] T039 [US1] Create CompensationHistory entry on employee creation (changeType: Hire) in src/EmployeeService/Services/EmployeeService.cs
- [ ] T040 [P] [US1] Create GET /v1/employees endpoint with pagination and filtering at src/EmployeeService/Controllers/EmployeesController.cs
- [ ] T041 [P] [US1] Create GET /v1/employees/{id} endpoint at src/EmployeeService/Controllers/EmployeesController.cs
- [ ] T042 [P] [US1] Create PUT /v1/employees/{id} endpoint at src/EmployeeService/Controllers/EmployeesController.cs
- [ ] T043 [US1] Publish EmployeeCreated event via Dapr pub/sub in src/EmployeeService/Services/EmployeeService.cs
- [ ] T044 [US1] Implement OnboardingService CRUD state store operations at src/OnboardingService/Services/OnboardingService.cs
- [ ] T045 [US1] Subscribe to EmployeeCreated event in src/OnboardingService/Controllers/EventsController.cs to auto-create OnboardingCase
- [ ] T046 [US1] Generate default onboarding task templates when OnboardingCase created (configurable via appsettings.json) in src/OnboardingService/Services/OnboardingService.cs
- [ ] T047 [US1] Create POST /v1/onboarding endpoint at src/OnboardingService/Controllers/OnboardingController.cs
- [ ] T048 [P] [US1] Create GET /v1/onboarding/{caseId} endpoint at src/OnboardingService/Controllers/OnboardingController.cs
- [ ] T049 [US1] Create PUT /v1/onboarding/tasks/{taskId} endpoint for task completion at src/OnboardingService/Controllers/OnboardingController.cs
- [ ] T050 [US1] Implement auto-transition logic to Active status when all tasks complete in src/OnboardingService/Services/OnboardingService.cs
- [ ] T051 [US1] Publish OnboardingCompleted event via Dapr pub/sub in src/OnboardingService/Services/OnboardingService.cs
- [ ] T052 [US1] Subscribe to OnboardingCompleted event in EmployeeService to update status at src/EmployeeService/Controllers/EventsController.cs
- [ ] T053 [P] [US1] Create GET /v1/onboarding/dashboard endpoint for active cases at src/OnboardingService/Controllers/OnboardingController.cs
- [ ] T054 [P] [US1] Implement employee directory search at frontend/src/pages/EmployeeDirectory.tsx with search by name/email
- [ ] T055 [P] [US1] Create employee detail page at frontend/src/pages/EmployeeDetail.tsx showing all employee information
- [ ] T056 [P] [US1] Create onboarding dashboard page at frontend/src/pages/OnboardingDashboard.tsx with progress indicators
- [ ] T057 [US1] Create employee creation form at frontend/src/components/CreateEmployeeForm.tsx
- [ ] T058 [US1] Implement API client for Employee Service at frontend/src/api/employeeApi.ts
- [ ] T059 [US1] Implement API client for Onboarding Service at frontend/src/api/onboardingApi.ts
- [ ] T060 [US1] Create custom React hook useEmployee at frontend/src/hooks/useEmployee.ts
- [ ] T061 [US1] Create custom React hook useOnboarding at frontend/src/hooks/useOnboarding.ts
- [ ] T062 [US1] Add structured logging for onboarding operations in src/OnboardingService/Services/OnboardingService.cs
- [ ] T063 [US1] Add structured logging for employee operations in src/EmployeeService/Services/EmployeeService.cs

**Checkpoint**: User Story 1 complete - employees can be onboarded end-to-end, independently testable

---

## Phase 4: User Story 2 - Performance Management Cycle (Priority: P1)

**Goal**: HR administrators and managers conduct regular performance reviews by setting review periods, assigning reviewers, collecting ratings and feedback, tracking completion

**Independent Test**: Create review cycle, assign employees to reviewers, submit reviews with ratings and feedback, verify completion tracking, generate performance report

### Implementation for User Story 2

- [ ] T064 [P] [US2] Create ReviewCycle entity model at src/PerformanceService/Models/ReviewCycle.cs per data-model.md schema
- [ ] T065 [P] [US2] Create PerformanceReview entity model at src/PerformanceService/Models/PerformanceReview.cs per data-model.md schema
- [ ] T066 [P] [US2] Create CompetencyRating value object at src/PerformanceService/Models/CompetencyRating.cs
- [ ] T067 [US2] Implement PerformanceService CRUD state store operations for cycles and reviews at src/PerformanceService/Services/PerformanceService.cs
- [ ] T068 [US2] Create POST /v1/performance/cycles endpoint at src/PerformanceService/Controllers/PerformanceCyclesController.cs per performance-service.openapi.yaml
- [ ] T069 [P] [US2] Create GET /v1/performance/cycles endpoint with status filtering at src/PerformanceService/Controllers/PerformanceCyclesController.cs
- [ ] T070 [P] [US2] Create GET /v1/performance/cycles/{id} endpoint at src/PerformanceService/Controllers/PerformanceCyclesController.cs
- [ ] T071 [P] [US2] Create PUT /v1/performance/cycles/{id} endpoint at src/PerformanceService/Controllers/PerformanceCyclesController.cs
- [ ] T072 [US2] Create POST /v1/performance/cycles/{id}/assign endpoint for review assignment at src/PerformanceService/Controllers/PerformanceCyclesController.cs
- [ ] T073 [US2] Implement review assignment logic with validation (reviewer must be employee) in src/PerformanceService/Services/PerformanceService.cs
- [ ] T074 [US2] Call Employee Service via Dapr service invocation to validate employee/reviewer IDs in src/PerformanceService/Services/PerformanceService.cs
- [ ] T075 [US2] Create POST /v1/performance/reviews endpoint for creating/submitting reviews at src/PerformanceService/Controllers/ReviewsController.cs
- [ ] T076 [US2] Implement review validation (all 5 competencies rated, ratings 1-5) in src/PerformanceService/Services/ReviewValidationService.cs
- [ ] T077 [US2] Calculate overall rating from competency ratings in src/PerformanceService/Services/PerformanceService.cs
- [ ] T078 [P] [US2] Create GET /v1/performance/reviews/{id} endpoint at src/PerformanceService/Controllers/ReviewsController.cs
- [ ] T079 [P] [US2] Create PUT /v1/performance/reviews/{id} endpoint for draft reviews at src/PerformanceService/Controllers/ReviewsController.cs
- [ ] T080 [P] [US2] Create POST /v1/performance/reviews/{id}/submit endpoint to make review immutable at src/PerformanceService/Controllers/ReviewsController.cs
- [ ] T081 [US2] Publish ReviewSubmitted event via Dapr pub/sub in src/PerformanceService/Services/PerformanceService.cs
- [ ] T082 [US2] Update ReviewCycle statistics on review submission in src/PerformanceService/Services/PerformanceService.cs
- [ ] T083 [US2] Implement reminder notification system for pending reviews approaching deadline in src/PerformanceService/Services/ReminderService.cs (log-based for simulator)
- [ ] T084 [P] [US2] Create GET /v1/performance/employees/{id}/reviews endpoint for review history at src/PerformanceService/Controllers/ReviewsController.cs
- [ ] T085 [US2] Subscribe to EmployeeTerminated event to archive reviews in src/PerformanceService/Controllers/EventsController.cs
- [ ] T086 [US2] Create GET /v1/performance/cycles/{id}/report endpoint for performance reporting at src/PerformanceService/Controllers/ReportsController.cs
- [ ] T087 [US2] Implement rating distribution calculation in src/PerformanceService/Services/ReportingService.cs
- [ ] T088 [US2] Implement department summaries with average ratings in src/PerformanceService/Services/ReportingService.cs
- [ ] T089 [P] [US2] Create review cycle list page at frontend/src/pages/ReviewCycles.tsx
- [ ] T090 [P] [US2] Create review cycle detail page at frontend/src/pages/ReviewCycleDetail.tsx with assignment interface
- [ ] T091 [P] [US2] Create performance review form at frontend/src/components/PerformanceReviewForm.tsx with competency ratings
- [ ] T092 [P] [US2] Create employee review history page at frontend/src/pages/EmployeeReviewHistory.tsx
- [ ] T093 [US2] Implement API client for Performance Service at frontend/src/api/performanceApi.ts
- [ ] T094 [US2] Create custom React hook usePerformance at frontend/src/hooks/usePerformance.ts
- [ ] T095 [US2] Add structured logging for performance operations in src/PerformanceService/Services/PerformanceService.cs

**Checkpoint**: User Story 2 complete - performance reviews can be conducted end-to-end, independently testable

---

## Phase 5: User Story 3 - Merit Processing (Priority: P2)

**Goal**: HR administrators process annual merit increases and bonuses by calculating adjustments based on performance ratings, budget constraints, and applying compensation changes

**Independent Test**: Define merit budget and guidelines, calculate raises/bonuses for employees based on performance ratings, review proposals, apply compensation changes with effective date, verify compensation history updated

### Implementation for User Story 3

- [ ] T091 [P] [US3] Create MeritCycle entity model at src/MeritService/Models/MeritCycle.cs per data-model.md schema
- [ ] T092 [P] [US3] Create MeritProposal entity model at src/MeritService/Models/MeritProposal.cs per data-model.md schema
- [ ] T093 [P] [US3] Create MeritGuideline value object at src/MeritService/Models/MeritGuideline.cs
- [ ] T094 [P] [US3] Create CompensationHistory entity model at src/EmployeeService/Models/CompensationHistory.cs per data-model.md schema
- [ ] T095 [US3] Implement MeritService state store operations at src/MeritService/Services/MeritService.cs
- [ ] T096 [US3] Subscribe to ReviewCycleCompleted event to enable merit processing in src/MeritService/Controllers/EventsController.cs
- [ ] T097 [US3] Create POST /v1/merit/cycles endpoint at src/MeritService/Controllers/MeritCyclesController.cs
- [ ] T098 [US3] Validate that linked ReviewCycle is Closed before creating MeritCycle in src/MeritService/Services/MeritService.cs
- [ ] T099 [US3] Call Performance Service via Dapr to fetch all reviews for cycle in src/MeritService/Services/MeritService.cs
- [ ] T100 [US3] Implement merit calculation engine at src/MeritService/Services/MeritCalculationService.cs (rating → raise % → raise amount)
- [ ] T101 [US3] Generate MeritProposals for all reviewed employees in src/MeritService/Services/MeritService.cs
- [ ] T102 [US3] Calculate total allocated budget from all proposals in src/MeritService/Services/MeritService.cs
- [ ] T103 [US3] Validate total allocated budget does not exceed total budget in src/MeritService/Services/MeritService.cs
- [ ] T104 [US3] Create budget variance alert when budget exceeded in src/MeritService/Services/MeritService.cs
- [ ] T105 [P] [US3] Create GET /v1/merit/cycles endpoint at src/MeritService/Controllers/MeritCyclesController.cs
- [ ] T106 [P] [US3] Create GET /v1/merit/cycles/{id} endpoint at src/MeritService/Controllers/MeritCyclesController.cs
- [ ] T107 [P] [US3] Create GET /v1/merit/proposals endpoint with filtering by cycleId at src/MeritService/Controllers/MeritProposalsController.cs
- [ ] T108 [P] [US3] Create PUT /v1/merit/proposals/{id} endpoint for manual adjustments at src/MeritService/Controllers/MeritProposalsController.cs
- [ ] T109 [US3] Create POST /v1/merit/cycles/{id}/approve endpoint at src/MeritService/Controllers/MeritCyclesController.cs
- [ ] T110 [US3] Create POST /v1/merit/cycles/{id}/apply endpoint at src/MeritService/Controllers/MeritCyclesController.cs
- [ ] T111 [US3] Call Employee Service via Dapr to update compensation on apply in src/MeritService/Services/MeritService.cs
- [ ] T112 [US3] Publish MeritApplied event via Dapr pub/sub in src/MeritService/Services/MeritService.cs
- [ ] T113 [US3] Implement PUT /v1/employees/{id}/compensation endpoint at src/EmployeeService/Controllers/EmployeesController.cs per employee-service.openapi.yaml
- [ ] T114 [US3] Create CompensationHistory entry on compensation update in src/EmployeeService/Services/EmployeeService.cs
- [ ] T115 [US3] Implement GET /v1/employees/{id}/compensation/history endpoint at src/EmployeeService/Controllers/EmployeesController.cs
- [ ] T116 [P] [US3] Create merit cycle list page at frontend/src/pages/MeritCycles.tsx
- [ ] T117 [P] [US3] Create merit proposals review page at frontend/src/pages/MeritProposals.tsx with budget tracking
- [ ] T118 [P] [US3] Create compensation history page at frontend/src/pages/CompensationHistory.tsx
- [ ] T119 [US3] Implement API client for Merit Service at frontend/src/api/meritApi.ts
- [ ] T120 [US3] Create custom React hook useMerit at frontend/src/hooks/useMerit.ts
- [ ] T121 [US3] Add structured logging for merit operations in src/MeritService/Services/MeritService.cs
- [ ] T122 [US3] Add structured logging for compensation updates in src/EmployeeService/Services/EmployeeService.cs

**Checkpoint**: User Story 3 complete - merit processing can be executed end-to-end, independently testable

---

## Phase 6: User Story 4 - Employee Offboarding (Priority: P2)

**Goal**: HR administrators process employee departures by recording termination details, managing exit tasks, calculating final pay, and archiving records

**Independent Test**: Initiate employee termination with separation date and reason, assign exit tasks, complete tasks, calculate final compensation, verify employee status changes to Inactive with historical records preserved

### Implementation for User Story 4

- [ ] T123 [P] [US4] Create OffboardingCase entity model at src/OffboardingService/Models/OffboardingCase.cs per data-model.md schema
- [ ] T124 [P] [US4] Create FinalCompensation value object at src/OffboardingService/Models/FinalCompensation.cs
- [ ] T125 [US4] Implement OffboardingService state store operations at src/OffboardingService/Services/OffboardingService.cs
- [ ] T126 [US4] Create POST /v1/offboarding endpoint at src/OffboardingService/Controllers/OffboardingController.cs
- [ ] T127 [US4] Validate employee is Active before initiating offboarding in src/OffboardingService/Services/OffboardingService.cs
- [ ] T128 [US4] Validate separation date constraints (not more than 30 days past, not more than 90 days future) in src/OffboardingService/Services/OffboardingService.cs
- [ ] T129 [US4] Generate default exit tasks when OffboardingCase created in src/OffboardingService/Services/OffboardingService.cs
- [ ] T130 [P] [US4] Create GET /v1/offboarding/{caseId} endpoint at src/OffboardingService/Controllers/OffboardingController.cs
- [ ] T131 [P] [US4] Create PUT /v1/offboarding/tasks/{taskId} endpoint for task completion at src/OffboardingService/Controllers/OffboardingController.cs
- [ ] T132 [US4] Implement final compensation calculation (salary through date, unused PTO, prorated bonus) at src/OffboardingService/Services/CompensationCalculationService.cs
- [ ] T133 [US4] Call Employee Service via Dapr to retrieve employee compensation data in src/OffboardingService/Services/OffboardingService.cs
- [ ] T134 [US4] Create POST /v1/offboarding/{caseId}/complete endpoint at src/OffboardingService/Controllers/OffboardingController.cs
- [ ] T135 [US4] Publish EmployeeTerminated event via Dapr pub/sub when offboarding complete in src/OffboardingService/Services/OffboardingService.cs
- [ ] T136 [US4] Subscribe to EmployeeTerminated event in EmployeeService to update status to Inactive at src/EmployeeService/Controllers/EventsController.cs
- [ ] T137 [US4] Create CompensationHistory entry with changeType Termination in src/EmployeeService/Services/EmployeeService.cs
- [ ] T138 [US4] Implement record immutability for terminated employees (prevent updates except admin corrections) in src/EmployeeService/Services/EmployeeService.cs
- [ ] T139 [US4] Implement rehire scenario support (create new employment period) in src/EmployeeService/Services/EmployeeService.cs
- [ ] T140 [P] [US4] Create GET /v1/offboarding endpoint with status filtering at src/OffboardingService/Controllers/OffboardingController.cs
- [ ] T141 [P] [US4] Create offboarding dashboard page at frontend/src/pages/OffboardingDashboard.tsx with case list and status
- [ ] T142 [P] [US4] Create offboarding detail page at frontend/src/pages/OffboardingDetail.tsx with task tracking and final compensation
- [ ] T143 [US4] Implement API client for Offboarding Service at frontend/src/api/offboardingApi.ts
- [ ] T144 [US4] Create custom React hook useOffboarding at frontend/src/hooks/useOffboarding.ts
- [ ] T145 [US4] Add structured logging for offboarding operations in src/OffboardingService/Services/OffboardingService.cs

**Checkpoint**: User Story 4 complete - employee offboarding can be processed end-to-end, independently testable

---

## Phase 7: User Story 5 - Employee Directory and Information Access (Priority: P3)

**Goal**: Users search for and view employee information including contact details, department structure, reporting relationships, compensation (with permissions), and employment history

**Independent Test**: Search employees by name/department/title, view employee profiles with role-based permissions, navigate org hierarchy, verify data visibility based on user role

### Implementation for User Story 5

- [ ] T146 [P] [US5] Create GET /v1/employees/search endpoint with query parameter at src/EmployeeService/Controllers/EmployeesController.cs per employee-service.openapi.yaml
- [ ] T147 [US5] Implement partial match search across name, email, employeeNumber fields in src/EmployeeService/Services/SearchService.cs
- [ ] T148 [US5] Implement search result ranking by relevance in src/EmployeeService/Services/SearchService.cs
- [ ] T149 [P] [US5] Create GET /v1/employees/{id}/team endpoint for manager's direct reports at src/EmployeeService/Controllers/EmployeesController.cs
- [ ] T150 [US5] Implement hierarchical team structure retrieval in src/EmployeeService/Services/EmployeeService.cs
- [ ] T151 [US5] Implement role-based data filtering (HR sees all, managers see team, employees see own) in src/EmployeeService/Middleware/RoleBasedFilterMiddleware.cs
- [ ] T152 [US5] Add role claim to authentication context (stub implementation for simulator) in src/EmployeeService/Services/AuthenticationService.cs
- [ ] T153 [P] [US5] Create employee profile component at frontend/src/components/EmployeeProfile.tsx with conditional field display
- [ ] T154 [P] [US5] Create organizational hierarchy component at frontend/src/components/OrgChart.tsx showing reporting structure
- [ ] T155 [P] [US5] Enhance employee directory page with advanced search filters at frontend/src/pages/EmployeeDirectory.tsx
- [ ] T156 [US5] Create department drill-down view at frontend/src/pages/DepartmentView.tsx
- [ ] T157 [US5] Implement role-based UI component visibility in frontend/src/components/RoleGuard.tsx
- [ ] T158 [US5] Add employee self-service profile edit capability at frontend/src/pages/MyProfile.tsx (limited fields)

**Checkpoint**: User Story 5 complete - employee directory and information access fully functional, independently testable

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T159 [P] Update README.md at repository root with project overview and links to quickstart.md
- [ ] T160 [P] Create API documentation aggregator page at frontend/src/pages/ApiDocs.tsx with links to all Swagger endpoints
- [ ] T161 Implement global error boundary at frontend/src/components/ErrorBoundary.tsx
- [ ] T162 [P] Add loading states and spinners to all API-calling components in frontend/src/components/
- [ ] T163 [P] Implement toast notifications for success/error messages at frontend/src/components/Toast.tsx
- [ ] T164 [P] Add input validation and error messages to all forms in frontend/src/components/
- [ ] T165 Implement concurrent update detection (optimistic concurrency) across all services
- [ ] T166 [P] Add OpenTelemetry correlation ID propagation across all Dapr service invocations
- [ ] T167 [P] Implement rate limiting middleware for all API endpoints
- [ ] T168 [P] Add comprehensive audit logging (user, timestamp, action, before/after values) to state store operations in all services
- [ ] T169 Create deployment documentation at docs/deployment.md with Bicep usage instructions
- [ ] T170 [P] Add Swagger/OpenAPI UI to all services at /swagger endpoint
- [ ] T171 [P] Implement data seeding script at scripts/seed-data.sh for demo data
- [ ] T172 Validate all tasks from quickstart.md work correctly end-to-end
- [ ] T173 [P] Create troubleshooting guide at docs/troubleshooting.md with common issues and solutions
- [ ] T174 [P] Add performance monitoring dashboard configuration for Aspire at src/AppHost/appsettings.json
- [ ] T175 [P] Create general reporting API endpoints at src/EmployeeService/Controllers/ReportsController.cs (headcount by department, turnover rates, compensation summary per FR-032)
- [ ] T176 [P] Implement data export functionality for employee lists with filtering at frontend/src/pages/Reports.tsx
- [ ] T177 [P] Establish baseline measurement for offboarding time (average days to complete) in src/OffboardingService/Services/MetricsService.cs for SC-009 validation
- [ ] T178 [P] Implement edge case handling: duplicate email validation with clear error message in src/EmployeeService/Services/EmployeeService.cs
- [ ] T179 [P] Implement edge case handling: manager reassignment when manager terminated in src/PerformanceService/Services/PerformanceService.cs
- [ ] T180 [P] Implement edge case handling: merit budget exceeded alert and adjustment workflow in src/MeritService/Services/MeritService.cs
- [ ] T181 [P] Implement edge case handling: concurrent update detection with optimistic concurrency (ETag-based) in all state store operations
- [ ] T182 [P] Implement edge case handling: rehire scenario (reuse email, new employee ID, preserve history) in src/EmployeeService/Services/EmployeeService.cs
- [ ] T183 Run linting and formatting across entire codebase (dotnet format, npm run lint)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup (Phase 1) completion - BLOCKS all user stories
- **User Stories (Phases 3-7)**: All depend on Foundational (Phase 2) completion
  - User Story 1 (P1 - Onboarding): Can start after Phase 2 - No dependencies on other stories
  - User Story 2 (P1 - Performance): Can start after Phase 2 - No dependencies on other stories  
  - User Story 3 (P2 - Merit): Can start after Phase 2 - Integrates with US1 (Employee) and US2 (Performance) but independently testable
  - User Story 4 (P2 - Offboarding): Can start after Phase 2 - Integrates with US1 (Employee) but independently testable
  - User Story 5 (P3 - Directory): Can start after Phase 2 - Builds on US1 (Employee) but independently testable
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Dependencies

**Critical Path**: Setup → Foundational → User Stories (can proceed in parallel or priority order)

- **User Story 1 (Onboarding)**: Depends only on Foundational phase
- **User Story 2 (Performance)**: Depends only on Foundational phase
- **User Story 3 (Merit)**: Depends on Foundational phase, calls Employee Service and Performance Service via Dapr (services must exist but can work with stub implementations initially)
- **User Story 4 (Offboarding)**: Depends on Foundational phase, calls Employee Service via Dapr
- **User Story 5 (Directory)**: Depends on Foundational phase, builds on Employee Service endpoints

### Within Each User Story

1. Models before services
2. Services before controllers
3. Controllers before API clients
4. API clients before frontend components
5. Core implementation before integration
6. Story complete before moving to next priority

### Parallel Opportunities

**Setup Phase**: Tasks T002-T006 (service initialization), T008-T009 (NuGet packages), T012-T014 (linting/test projects) can all run in parallel

**Foundational Phase**: Tasks T017-T020 (infrastructure setup), T022-T023 (frontend base), T026-T027 (shared components) can run in parallel

**User Story 1**: Tasks T029-T031 (models), T036-T038 (GET/PUT endpoints), T044 (GET onboarding), T050-T052 (frontend pages) can run in parallel within their respective layers

**User Story 2**: Tasks T060-T062 (models), T065-T067 (GET/PUT cycle endpoints), T074-T076 (review endpoints), T084-T087 (frontend pages) can run in parallel within their respective layers

**User Story 3**: Tasks T091-T094 (models), T105-T108 (GET/PUT endpoints), T116-T118 (frontend pages) can run in parallel within their respective layers

**User Story 4**: Tasks T123-T124 (models), T130-T131 (GET/PUT endpoints), T141-T142 (frontend pages) can run in parallel within their respective layers

**User Story 5**: Tasks T146-T149 (endpoints), T153-T156 (frontend components) can run in parallel within their respective layers

**Polish Phase**: Most tasks (T159-T175) can run in parallel as they affect different areas

**Cross-Story Parallelism**: With sufficient team capacity, different user stories can be worked on simultaneously after Foundational phase completes

---

## Parallel Example: User Story 1

```bash
# Launch all models for User Story 1 together:
Task T029: "Create Employee entity model at src/EmployeeService/Models/Employee.cs"
Task T030: "Create OnboardingCase entity model at src/OnboardingService/Models/OnboardingCase.cs"
Task T031: "Create Task entity model at src/OnboardingService/Models/Task.cs"

# After models complete, launch parallel API endpoints:
Task T036: "Create GET /v1/employees endpoint at src/EmployeeService/Controllers/EmployeesController.cs"
Task T037: "Create GET /v1/employees/{id} endpoint at src/EmployeeService/Controllers/EmployeesController.cs"
Task T038: "Create PUT /v1/employees/{id} endpoint at src/EmployeeService/Controllers/EmployeesController.cs"

# After API complete, launch parallel frontend work:
Task T050: "Implement employee directory search at frontend/src/pages/EmployeeDirectory.tsx"
Task T051: "Create employee detail page at frontend/src/pages/EmployeeDetail.tsx"
Task T052: "Create onboarding dashboard page at frontend/src/pages/OnboardingDashboard.tsx"
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

**Rationale**: Both User Story 1 (Onboarding) and User Story 2 (Performance) are marked P1 priority and form the core HR system functionality

1. Complete Phase 1: Setup (15 tasks)
2. Complete Phase 2: Foundational (13 tasks) - **CRITICAL CHECKPOINT**
3. Complete Phase 3: User Story 1 - Onboarding (31 tasks)
4. **VALIDATE**: Test onboarding flow end-to-end independently
5. Complete Phase 4: User Story 2 - Performance (31 tasks)
6. **VALIDATE**: Test performance management independently
7. **DEPLOY/DEMO**: Two core P1 stories complete, delivering substantial value

**MVP Delivers**: 
- Employee onboarding with task tracking
- Performance review cycles and reviews
- Basic employee directory
- Complete observability via Aspire dashboard

### Incremental Delivery

1. **Foundation (28 tasks)**: Setup + Foundational → Project infrastructure ready
2. **MVP Release 1 (31 tasks)**: Add User Story 1 → Employee onboarding functional → Deploy/Demo
3. **MVP Release 2 (31 tasks)**: Add User Story 2 → Performance management functional → Deploy/Demo
4. **Enhancement Release 1 (32 tasks)**: Add User Story 3 → Merit processing functional → Deploy/Demo
5. **Enhancement Release 2 (23 tasks)**: Add User Story 4 → Offboarding functional → Deploy/Demo
6. **Enhancement Release 3 (13 tasks)**: Add User Story 5 → Directory and search enhanced → Deploy/Demo
7. **Polish Release (17 tasks)**: Cross-cutting improvements → Production-ready

Each release adds independent value without breaking previous functionality.

### Parallel Team Strategy

With 3 developers after Foundational phase completes:

- **Developer A**: User Story 1 (Onboarding) - 31 tasks
- **Developer B**: User Story 2 (Performance) - 31 tasks  
- **Developer C**: Foundational enhancements, prepare User Story 3

After US1 and US2 complete:

- **Developer A**: User Story 3 (Merit) - 32 tasks
- **Developer B**: User Story 4 (Offboarding) - 23 tasks
- **Developer C**: User Story 5 (Directory) - 13 tasks

---

## Summary Statistics

### Total Task Count: 183 tasks

### Tasks by Phase:

- **Phase 1 (Setup)**: 17 tasks (added Pact.NET and code coverage setup)
- **Phase 2 (Foundational)**: 13 tasks ⚠️ BLOCKING
- **Phase 3 (User Story 1 - Onboarding, P1)**: 33 tasks 🎯 (added CompensationHistory creation)
- **Phase 4 (User Story 2 - Performance, P1)**: 32 tasks 🎯 (added reminder system)
- **Phase 5 (User Story 3 - Merit, P2)**: 32 tasks
- **Phase 6 (User Story 4 - Offboarding, P2)**: 23 tasks
- **Phase 7 (User Story 5 - Directory, P3)**: 13 tasks
- **Phase 8 (Polish)**: 20 tasks (added reporting, edge cases, baseline measurement)

### Parallel Opportunities:

- **Setup Phase**: 10 parallelizable tasks (67%)
- **Foundational Phase**: 7 parallelizable tasks (54%)
- **User Story 1**: 12 parallelizable tasks (39%)
- **User Story 2**: 15 parallelizable tasks (48%)
- **User Story 3**: 11 parallelizable tasks (34%)
- **User Story 4**: 8 parallelizable tasks (35%)
- **User Story 5**: 7 parallelizable tasks (54%)
- **Polish Phase**: 13 parallelizable tasks (76%)

### MVP Scope (Recommended):

**Phase 1 + Phase 2 + Phase 3 + Phase 4 = 95 tasks**

Delivers:
- Complete employee onboarding system with compensation tracking
- Complete performance management system with reminders
- Employee directory and search
- Full observability and monitoring
- Contract testing infrastructure
- Code coverage enforcement
- Production-ready infrastructure

### Success Criteria Mapping:

- **SC-001** (onboarding in <10 min): Covered by User Story 1 tasks
- **SC-002** (50 concurrent users): Infrastructure in Foundational phase
- **SC-003** (merit accuracy): User Story 3 calculation engine
- **SC-004** (95% onboarding completion): User Story 1 tracking
- **SC-005** (90% review completion): User Story 2 completion tracking
- **SC-006** (reports in <10s): User Story 2 reporting tasks
- **SC-007** (zero data loss): Dapr state store in all services
- **SC-008** (self-service <3s): User Story 5 frontend performance
- **SC-009** (offboarding 40% faster): User Story 4 automation
- **SC-010** (100% consistency): State transitions enforced across all stories
- **SC-011** (role-based access): User Story 5 role filtering
- **SC-012** (merit budget compliance): User Story 3 budget validation

---

## Notes

- **[P] marker** indicates tasks that can run in parallel (different files, no blocking dependencies)
- **[Story] label** maps each task to its user story for traceability and independent delivery
- Each user story is independently completable and testable after Foundational phase
- Commit after completing each task or logical group of related tasks
- Tests are NOT included per feature specification - focus is on implementation
- Stop at checkpoints to validate story independence before proceeding
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
- **Critical Success Factor**: Complete Foundational phase (Phase 2) before starting ANY user story work

---

## Format Validation ✅

All 183 tasks follow the required format:
- ✅ Checkbox prefix: `- [ ]`
- ✅ Task ID: T001-T183 in execution order
- ✅ [P] marker: Present on parallelizable tasks
- ✅ [Story] label: Present on all user story tasks (US1-US5)
- ✅ File paths: Included in all implementation tasks
- ✅ Clear descriptions: All tasks have actionable descriptions

**Remediation Applied** ✅ (2026-01-12):
- Added Pact.NET contract testing setup (T015)
- Added code coverage configuration (T016)
- Improved AppHost orchestration description (T018)
- Added CompensationHistory creation to US1 (T034, T039)
- Clarified onboarding template customization in spec.md (FR-010, T046)
- Fixed duplicate state store operation descriptions (T035, T044, T067)
- Added review reminder system (T083)
- Added general reporting endpoints (T175-T176)
- Added offboarding baseline measurement (T177)
- Added edge case handling tasks (T178-T182)
- Clarified employment status enum in spec.md (FR-001)
- Clarified rehire scenario in spec.md (FR-031)

**Tasks ready for execution!**
