# Data Model: HR System Simulator

**Date**: 2026-01-09  
**Status**: Phase 1 Complete

## Overview

This document defines the data model for the HR System Simulator, including entity schemas, relationships, validation rules, and state transitions. The model is designed for storage in Dapr state stores (Redis for local development, Azure Cosmos DB for production) with document-oriented patterns.

---

## Entity Schemas

### 1. Employee

**Description**: Represents an individual working at the firm with complete employment information.

**Storage Key**: `employee:{employeeId}`

**Schema**:
```json
{
  "id": "string (UUID)",
  "employeeNumber": "string (unique, e.g., 'EMP-1001')",
  "personalInfo": {
    "firstName": "string",
    "lastName": "string",
    "email": "string (unique, validated)",
    "phoneNumber": "string (optional)",
    "address": {
      "street": "string",
      "city": "string",
      "state": "string",
      "zipCode": "string"
    }
  },
  "employmentInfo": {
    "hireDate": "date (ISO 8601)",
    "jobTitle": "string",
    "department": "string (references Department.name)",
    "managerId": "string (references Employee.id, optional)",
    "employmentType": "enum (FullTime, PartTime, Contract)",
    "status": "enum (Onboarding, Active, Inactive)"
  },
  "compensation": {
    "salaryType": "enum (Annual, Hourly)",
    "currentSalary": "decimal (2 decimal places)",
    "currency": "string (default: 'USD')",
    "bonusTarget": "decimal (optional, percentage)"
  },
  "metadata": {
    "createdAt": "datetime (ISO 8601)",
    "updatedAt": "datetime (ISO 8601)",
    "createdBy": "string (user ID)",
    "lastModifiedBy": "string (user ID)"
  }
}
```

**Validation Rules**:
- `email` must be unique across all employees (enforced by lookup index)
- `employeeNumber` must be unique and auto-generated on creation
- `hireDate` cannot be in the future
- `currentSalary` must be greater than 0
- `status` transitions: Onboarding → Active → Inactive (one-way)

**Indexes Required**:
- `email-index` (for uniqueness check)
- `department-index:{department}` → list of employee IDs (for department queries)
- `manager-index:{managerId}` → list of employee IDs (for team queries)
- `status-index:{status}` → list of employee IDs (for active/inactive filtering)

---

### 2. OnboardingCase

**Description**: Represents a new employee onboarding process with associated tasks.

**Storage Key**: `onboarding:{caseId}`

**Schema**:
```json
{
  "id": "string (UUID)",
  "employeeId": "string (references Employee.id)",
  "startDate": "date (ISO 8601)",
  "targetCompletionDate": "date (ISO 8601, optional)",
  "status": "enum (InProgress, Completed, Cancelled)",
  "tasks": [
    {
      "id": "string (UUID)",
      "description": "string",
      "taskType": "enum (Paperwork, Training, Equipment, Access, Other)",
      "assignedTo": "string (user ID or role)",
      "dueDate": "date (ISO 8601, optional)",
      "completedDate": "datetime (ISO 8601, nullable)",
      "completedBy": "string (user ID, nullable)",
      "status": "enum (Pending, InProgress, Completed)"
    }
  ],
  "notes": "string (optional)",
  "metadata": {
    "createdAt": "datetime (ISO 8601)",
    "updatedAt": "datetime (ISO 8601)",
    "completedAt": "datetime (ISO 8601, nullable)"
  }
}
```

**Validation Rules**:
- `employeeId` must reference an existing Employee with status "Onboarding"
- `startDate` should align with Employee.hireDate
- Cannot complete case if any task with status != Completed
- Auto-update `status` to Completed when all tasks are Completed

**State Transitions**:
```
InProgress → Completed (when all tasks done)
InProgress → Cancelled (manual cancellation)
```

**Indexes Required**:
- `onboarding-employee-index:{employeeId}` → caseId (one-to-one mapping)
- `onboarding-status-index:{status}` → list of case IDs (for dashboard queries)

---

### 3. Department

**Description**: Represents an organizational unit within the firm.

**Storage Key**: `department:{departmentId}`

**Schema**:
```json
{
  "id": "string (UUID)",
  "name": "string (unique)",
  "code": "string (unique, e.g., 'ENG', 'HR', 'FIN')",
  "description": "string (optional)",
  "departmentHeadId": "string (references Employee.id, optional)",
  "parentDepartmentId": "string (references Department.id, optional)",
  "metadata": {
    "createdAt": "datetime (ISO 8601)",
    "updatedAt": "datetime (ISO 8601)"
  }
}
```

**Validation Rules**:
- `name` must be unique
- `code` must be unique and uppercase
- `parentDepartmentId` cannot create circular references (validate on save)

**Indexes Required**:
- `department-name-index` (for uniqueness check)
- `department-code-index` (for uniqueness check)

---

### 4. ReviewCycle

**Description**: Represents a period for conducting performance reviews with tracking of assigned reviews.

**Storage Key**: `reviewcycle:{cycleId}`

**Schema**:
```json
{
  "id": "string (UUID)",
  "name": "string (e.g., '2026 Annual Review')",
  "reviewPeriodStart": "date (ISO 8601)",
  "reviewPeriodEnd": "date (ISO 8601)",
  "cycleStartDate": "date (ISO 8601)",
  "cycleEndDate": "date (ISO 8601)",
  "status": "enum (Draft, Active, Closed)",
  "assignedReviews": [
    {
      "employeeId": "string (references Employee.id)",
      "reviewerId": "string (references Employee.id)",
      "reviewType": "enum (Manager, Peer, Self)",
      "assigned": "boolean",
      "completed": "boolean"
    }
  ],
  "statistics": {
    "totalAssigned": "integer",
    "totalCompleted": "integer",
    "completionPercentage": "decimal (calculated)"
  },
  "metadata": {
    "createdAt": "datetime (ISO 8601)",
    "updatedAt": "datetime (ISO 8601)",
    "closedAt": "datetime (ISO 8601, nullable)"
  }
}
```

**Validation Rules**:
- `reviewPeriodEnd` must be after `reviewPeriodStart`
- `cycleEndDate` must be after `cycleStartDate`
- Cannot modify `assignedReviews` when status is Closed
- Auto-calculate `statistics` on each update

**State Transitions**:
```
Draft → Active (when cycle starts)
Active → Closed (manual close after cycle end date)
```

**Indexes Required**:
- `reviewcycle-status-index:{status}` → list of cycle IDs
- `reviewcycle-active` → active cycle ID (for quick lookup)

---

### 5. PerformanceReview

**Description**: Represents a formal performance evaluation with ratings and feedback.

**Storage Key**: `review:{cycleId}:{employeeId}:{reviewerId}`

**Schema**:
```json
{
  "id": "string (UUID)",
  "employeeId": "string (references Employee.id)",
  "reviewCycleId": "string (references ReviewCycle.id)",
  "reviewerId": "string (references Employee.id)",
  "reviewType": "enum (Manager, Peer, Self)",
  "reviewPeriodStart": "date (ISO 8601, from cycle)",
  "reviewPeriodEnd": "date (ISO 8601, from cycle)",
  "competencyRatings": [
    {
      "competency": "enum (JobKnowledge, QualityOfWork, Communication, Teamwork, Leadership)",
      "rating": "integer (1-5)",
      "comments": "string (optional)"
    }
  ],
  "overallRating": "integer (1-5, calculated average or manual)",
  "writtenFeedback": {
    "strengths": "string",
    "areasForImprovement": "string",
    "developmentRecommendations": "string (optional)"
  },
  "status": "enum (Draft, Submitted)",
  "metadata": {
    "createdAt": "datetime (ISO 8601)",
    "submittedAt": "datetime (ISO 8601, nullable)"
  }
}
```

**Validation Rules**:
- All `competencyRatings` must have ratings between 1-5
- Cannot submit review with incomplete competency ratings
- `overallRating` can be calculated as average of competency ratings or set manually
- Once `status` is Submitted, cannot modify ratings or feedback

**State Transitions**:
```
Draft → Submitted (one-way, immutable after submission)
```

**Indexes Required**:
- `review-employee-index:{employeeId}` → list of review IDs (for performance history)
- `review-cycle-index:{cycleId}` → list of review IDs (for cycle reporting)
- `review-cycle-employee-index:{cycleId}:{employeeId}` → list of review IDs (for multi-reviewer scenarios)

---

### 6. MeritCycle

**Description**: Represents compensation adjustment processing linked to a performance review cycle.

**Storage Key**: `meritcycle:{cycleId}`

**Schema**:
```json
{
  "id": "string (UUID)",
  "name": "string (e.g., '2026 Merit Cycle')",
  "linkedReviewCycleId": "string (references ReviewCycle.id)",
  "totalBudget": "decimal (2 decimal places)",
  "allocatedBudget": "decimal (2 decimal places, calculated)",
  "meritGuidelines": [
    {
      "performanceRating": "integer (1-5)",
      "raisePercentage": "decimal (e.g., 5.0 for 5%)",
      "bonusPercentage": "decimal (optional)"
    }
  ],
  "effectiveDate": "date (ISO 8601)",
  "status": "enum (Draft, Calculating, PendingApproval, Approved, Applied)",
  "proposals": [
    {
      "employeeId": "string (references Employee.id)",
      "proposalId": "string (references MeritProposal.id)"
    }
  ],
  "metadata": {
    "createdAt": "datetime (ISO 8601)",
    "updatedAt": "datetime (ISO 8601)",
    "appliedAt": "datetime (ISO 8601, nullable)"
  }
}
```

**Validation Rules**:
- `linkedReviewCycleId` must reference a Closed ReviewCycle
- `allocatedBudget` cannot exceed `totalBudget` (enforced on approval)
- `meritGuidelines` must have entries for all rating values 1-5
- Cannot modify proposals after status is Approved

**State Transitions**:
```
Draft → Calculating (when generating proposals)
Calculating → PendingApproval (when proposals ready)
PendingApproval → Approved (after review, if budget valid)
PendingApproval → Draft (for adjustments)
Approved → Applied (when compensation updated)
```

**Indexes Required**:
- `meritcycle-status-index:{status}` → list of cycle IDs
- `meritcycle-review-index:{linkedReviewCycleId}` → cycle ID (one-to-one)

---

### 7. MeritProposal

**Description**: Represents a proposed compensation change for an employee as part of merit processing.

**Storage Key**: `meritproposal:{proposalId}`

**Schema**:
```json
{
  "id": "string (UUID)",
  "meritCycleId": "string (references MeritCycle.id)",
  "employeeId": "string (references Employee.id)",
  "performanceRating": "integer (1-5, from PerformanceReview)",
  "currentCompensation": {
    "salary": "decimal",
    "bonusTarget": "decimal (optional)"
  },
  "proposedChanges": {
    "raisePercentage": "decimal",
    "raiseAmount": "decimal (calculated)",
    "bonusAmount": "decimal (optional)",
    "newSalary": "decimal (calculated)"
  },
  "status": "enum (Proposed, Adjusted, Approved, Rejected)",
  "adjustmentNotes": "string (optional, for manual adjustments)",
  "metadata": {
    "createdAt": "datetime (ISO 8601)",
    "updatedAt": "datetime (ISO 8601)",
    "approvedAt": "datetime (ISO 8601, nullable)"
  }
}
```

**Validation Rules**:
- `newSalary` = `currentSalary` + `raiseAmount`
- `raiseAmount` = `currentSalary` × (`raisePercentage` / 100)
- Cannot approve proposal if MeritCycle budget exceeded
- Manual adjustments require `adjustmentNotes`

**State Transitions**:
```
Proposed → Adjusted (manual changes)
Proposed → Approved (no changes needed)
Adjusted → Approved (after review)
Proposed|Adjusted → Rejected (if necessary)
```

**Indexes Required**:
- `meritproposal-cycle-index:{meritCycleId}` → list of proposal IDs
- `meritproposal-employee-index:{employeeId}` → list of proposal IDs (history)

---

### 8. OffboardingCase

**Description**: Represents an employee departure process with exit tasks and final compensation calculations.

**Storage Key**: `offboarding:{caseId}`

**Schema**:
```json
{
  "id": "string (UUID)",
  "employeeId": "string (references Employee.id)",
  "separationDate": "date (ISO 8601)",
  "terminationType": "enum (Voluntary, Involuntary)",
  "reasonCode": "enum (Resignation, Retirement, Termination, Layoff, Other)",
  "notes": "string (optional, confidential)",
  "exitTasks": [
    {
      "id": "string (UUID)",
      "description": "string",
      "taskType": "enum (EquipmentReturn, AccessRevocation, ExitInterview, FinalPayment, Other)",
      "assignedTo": "string (user ID or role)",
      "dueDate": "date (ISO 8601, optional)",
      "completedDate": "datetime (ISO 8601, nullable)",
      "completedBy": "string (user ID, nullable)",
      "status": "enum (Pending, InProgress, Completed)"
    }
  ],
  "finalCompensation": {
    "salaryThroughDate": "decimal (calculated)",
    "unusedPTODays": "decimal",
    "unusedPTOValue": "decimal (calculated)",
    "proratedBonus": "decimal (optional)",
    "totalFinalPayment": "decimal (calculated)"
  },
  "status": "enum (InProgress, Completed)",
  "metadata": {
    "createdAt": "datetime (ISO 8601)",
    "updatedAt": "datetime (ISO 8601)",
    "completedAt": "datetime (ISO 8601, nullable)"
  }
}
```

**Validation Rules**:
- `employeeId` must reference an existing Active employee
- `separationDate` cannot be more than 30 days in the past or more than 90 days in the future
- Cannot complete case if any exit task with status != Completed
- Auto-calculate `finalCompensation.totalFinalPayment` on completion

**State Transitions**:
```
InProgress → Completed (when all tasks done and final payment calculated)
```

**Indexes Required**:
- `offboarding-employee-index:{employeeId}` → case ID (one-to-one)
- `offboarding-status-index:{status}` → list of case IDs

---

### 9. CompensationHistory

**Description**: Represents historical changes to employee compensation for audit and reporting.

**Storage Key**: `compensation-history:{employeeId}:{effectiveDate}`

**Schema**:
```json
{
  "id": "string (UUID)",
  "employeeId": "string (references Employee.id)",
  "effectiveDate": "date (ISO 8601)",
  "changeType": "enum (MeritIncrease, Promotion, MarketAdjustment, Hire, Termination)",
  "previousAmount": "decimal (nullable for first entry)",
  "newAmount": "decimal",
  "changeAmount": "decimal (calculated)",
  "changePercentage": "decimal (calculated, nullable)",
  "reason": "string (e.g., 'Annual Merit', 'Promotion to Senior Engineer')",
  "authorizedBy": "string (user ID)",
  "relatedDocumentId": "string (optional, e.g., MeritProposal.id)",
  "metadata": {
    "createdAt": "datetime (ISO 8601)"
  }
}
```

**Validation Rules**:
- `effectiveDate` cannot be in the future
- `newAmount` must be greater than 0
- `changeAmount` = `newAmount` - `previousAmount`
- `changePercentage` = (`changeAmount` / `previousAmount`) × 100
- Immutable after creation (audit trail)

**Indexes Required**:
- `compensation-history-employee-index:{employeeId}` → list of history IDs (chronological)

---

## Entity Relationships

### Relationship Diagram

```
Employee (1) ────┬──── (1) OnboardingCase
                 │
                 ├──── (*) PerformanceReview
                 │
                 ├──── (*) MeritProposal
                 │
                 ├──── (1) OffboardingCase
                 │
                 ├──── (*) CompensationHistory
                 │
                 └──── (1) Department

ReviewCycle (1) ────── (*) PerformanceReview
                │
                └──── (1) MeritCycle

MeritCycle (1) ────── (*) MeritProposal
```

### Relationship Rules

1. **Employee → OnboardingCase**: One-to-one (optional). An employee can have at most one active onboarding case.

2. **Employee → PerformanceReview**: One-to-many. An employee can have multiple reviews across different cycles and from different reviewers.

3. **Employee → MeritProposal**: One-to-many. An employee can have multiple merit proposals across different cycles.

4. **Employee → OffboardingCase**: One-to-one (optional). An employee can have at most one offboarding case.

5. **Employee → CompensationHistory**: One-to-many. All compensation changes tracked over time.

6. **Employee → Department**: Many-to-one. Each employee belongs to one department.

7. **Employee → Employee (Manager)**: Many-to-one (optional). Employees report to one manager (also an employee).

8. **ReviewCycle → PerformanceReview**: One-to-many. A cycle contains multiple reviews for different employees.

9. **ReviewCycle → MeritCycle**: One-to-one. A merit cycle is linked to one review cycle.

10. **MeritCycle → MeritProposal**: One-to-many. A merit cycle generates proposals for multiple employees.

---

## Data Consistency Rules

### Cross-Entity Constraints

1. **Employee Status Consistency**:
   - Employee with status "Onboarding" MUST have an active OnboardingCase
   - Employee with status "Inactive" MUST have a completed OffboardingCase
   - Changing Employee status to "Inactive" MUST trigger EmployeeTerminated event

2. **Review Cycle Closure**:
   - ReviewCycle can only be Closed if `completionPercentage` >= 80% (configurable threshold)
   - Closed ReviewCycle cannot have new reviews added

3. **Merit Cycle Budget**:
   - Sum of all MeritProposal.proposedChanges.raiseAmount in a cycle MUST NOT exceed MeritCycle.totalBudget
   - Budget validation occurs during approval transition

4. **Offboarding Completion**:
   - Completing OffboardingCase MUST update Employee.status to "Inactive"
   - Completing OffboardingCase MUST create CompensationHistory entry with changeType "Termination"

### Eventual Consistency Scenarios

Due to the distributed nature of microservices, some data may be eventually consistent:

1. **Employee Data in Other Services**:
   - Performance Service may cache employee names for display
   - Cache invalidated on EmployeeUpdated event
   - Acceptable lag: up to 5 minutes

2. **Review Completion Statistics**:
   - ReviewCycle.statistics recalculated periodically or on review submission
   - May be slightly out of sync during high submission volume
   - Acceptable for reporting purposes

3. **Merit Budget Allocation**:
   - MeritCycle.allocatedBudget recalculated when proposals change
   - Real-time accuracy critical for approval, eventually consistent for display

---

## State Management Patterns

### State Store Structure

Using Dapr state store with composite keys and index documents:

```
# Core entities
employee:{employeeId} → Employee document
onboarding:{caseId} → OnboardingCase document
department:{departmentId} → Department document
reviewcycle:{cycleId} → ReviewCycle document
review:{cycleId}:{employeeId}:{reviewerId} → PerformanceReview document
meritcycle:{cycleId} → MeritCycle document
meritproposal:{proposalId} → MeritProposal document
offboarding:{caseId} → OffboardingCase document
compensation-history:{employeeId}:{effectiveDate} → CompensationHistory document

# Index documents (for queries)
email-index:{email} → employeeId (string)
department-index:{department} → [employeeIds] (array)
manager-index:{managerId} → [employeeIds] (array)
status-index:{status} → [employeeIds] (array)
onboarding-status-index:{status} → [caseIds] (array)
review-employee-index:{employeeId} → [reviewIds] (array)
review-cycle-index:{cycleId} → [reviewIds] (array)
```

### Query Patterns

1. **Get Employee by Email**:
   - Lookup: `email-index:{email}` → employeeId
   - Retrieve: `employee:{employeeId}`

2. **Get All Active Employees**:
   - Lookup: `status-index:Active` → [employeeIds]
   - Retrieve: `employee:{employeeId}` for each ID

3. **Get Employee's Performance History**:
   - Lookup: `review-employee-index:{employeeId}` → [reviewIds]
   - Retrieve: `review:*` for each ID

4. **Get All Reviews in Cycle**:
   - Lookup: `review-cycle-index:{cycleId}` → [reviewIds]
   - Retrieve: `review:*` for each ID

---

## Performance Considerations

### Caching Strategy

1. **Employee Data**: Cache in Performance/Merit/Offboarding services for 5 minutes (invalidate on events)
2. **Department Data**: Cache for 1 hour (departments change infrequently)
3. **Active Review Cycle**: Cache for duration of cycle (invalidate on status change)

### Partitioning Strategy (Cosmos DB)

- **Partition Key**: `/{entityType}/{id}` (e.g., `/employee/123`)
- **Rationale**: Even distribution, supports cross-partition queries when needed
- **Alternative**: Partition by department for employee data (if department-based queries dominate)

### Query Optimization

1. Use index documents for common queries (avoid full scans)
2. Denormalize frequently accessed data (e.g., employee name in review documents)
3. Implement pagination for large result sets (e.g., all employees, all reviews)
4. Use Dapr query API with filters where supported by state store

---

## Migration and Versioning

### Schema Evolution

1. **Additive Changes** (backward compatible):
   - Add optional fields to entities
   - Add new entity types
   - Add new enum values (append only)

2. **Breaking Changes** (require migration):
   - Rename or remove fields
   - Change field types
   - Remove enum values
   - Change validation rules

### Versioning Strategy

- Include `_schemaVersion` field in all entities
- Current version: `1.0.0`
- On breaking changes, increment version and provide migration scripts
- Services must handle multiple schema versions during transition periods

---

## Security and Privacy

### Sensitive Data

1. **PII (Personal Information)**:
   - Employee.personalInfo (name, email, address, phone)
   - Encrypted at rest in Cosmos DB

2. **Confidential Data**:
   - OffboardingCase.notes (termination reasons)
   - PerformanceReview.writtenFeedback
   - Access restricted by role-based permissions

3. **Financial Data**:
   - Employee.compensation
   - MeritProposal.proposedChanges
   - CompensationHistory entries
   - Access restricted to HR administrators

### Audit Requirements

All entity modifications logged with:
- User ID (who)
- Timestamp (when)
- Action (create, update, delete)
- Before/after values (what changed)

Audit logs retained for 7 years for compliance.

---

## Next Steps (Phase 1)

- [x] Data model defined
- [ ] Generate API contracts (OpenAPI specs)
- [ ] Create quickstart guide
- [ ] Update agent context

**Data Model Complete**: Ready for contract generation.
