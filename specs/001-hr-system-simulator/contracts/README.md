# API Contracts

This directory contains OpenAPI 3.0 specifications for all microservices in the HR System Simulator.

## Services

### 1. Employee Service
**File**: `employee-service.openapi.yaml`  
**Port**: 5001 (local)  
**Endpoints**:
- `POST /v1/employees` - Create employee
- `GET /v1/employees` - List employees (with filters)
- `GET /v1/employees/{id}` - Get employee details
- `PUT /v1/employees/{id}` - Update employee
- `GET /v1/employees/search` - Search employees
- `PUT /v1/employees/{id}/compensation` - Update compensation
- `GET /v1/employees/{id}/compensation/history` - Compensation history

### 2. Performance Service
**File**: `performance-service.openapi.yaml`  
**Port**: 5003 (local)  
**Endpoints**:
- `POST /v1/performance/cycles` - Create review cycle
- `GET /v1/performance/cycles` - List cycles
- `POST /v1/performance/cycles/{id}/assign` - Assign reviews
- `POST /v1/performance/reviews` - Create/submit review
- `GET /v1/performance/employees/{id}/reviews` - Employee review history
- `GET /v1/performance/cycles/{id}/report` - Cycle performance report

### 3. Onboarding Service (Stub)
**Port**: 5002 (local)  
**Endpoints**: TBD in detailed implementation

### 4. Merit Service (Stub)
**Port**: 5004 (local)  
**Endpoints**: TBD in detailed implementation

### 5. Offboarding Service (Stub)
**Port**: 5005 (local)  
**Endpoints**: TBD in detailed implementation

## Contract Testing

All services use **Pact.NET** for consumer-driven contract testing:

1. **Consumer Tests** (Frontend): Define expected API contracts
2. **Provider Tests** (Backend): Verify service compliance with contracts
3. **Pact Broker**: Store and version contracts (optional, can use file-based)

## Usage

### View in Swagger UI

1. Install Swagger Editor: `npm install -g swagger-editor`
2. Run: `swagger-editor employee-service.openapi.yaml`

### Generate TypeScript Types

```bash
npx openapi-typescript employee-service.openapi.yaml --output ../frontend/src/types/employee-api.ts
```

### Generate .NET Client SDK

```bash
dotnet tool install -g Microsoft.dotnet-openapi
dotnet openapi add file employee-service.openapi.yaml
```

## Versioning

- All APIs versioned with `/v1` prefix
- Breaking changes increment version (e.g., `/v2`)
- Additive changes maintain backward compatibility within version

## Service Communication

- **Frontend → Backend**: RESTful HTTP calls to these OpenAPI endpoints
- **Backend → Backend**: Dapr service invocation (uses these endpoints)
- **Event-Driven**: Dapr pub/sub for async communication (events documented separately)

## Next Steps

- Complete OpenAPI specs for remaining 3 services (Onboarding, Merit, Offboarding)
- Generate TypeScript types for frontend
- Implement Pact consumer tests in React frontend
- Implement Pact provider tests in .NET services
