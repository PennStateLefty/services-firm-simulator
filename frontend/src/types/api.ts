// Auto-generated TypeScript types from OpenAPI specifications
// These will be regenerated when OpenAPI specs are finalized

export enum EmploymentStatus {
  Pending = 'Pending',
  Active = 'Active',
  OnLeave = 'OnLeave',
  Terminated = 'Terminated'
}

export enum CompetencyType {
  TechnicalExpertise = 'TechnicalExpertise',
  Leadership = 'Leadership',
  Communication = 'Communication',
  ProblemSolving = 'ProblemSolving',
  Collaboration = 'Collaboration',
  Innovation = 'Innovation',
  ClientFocus = 'ClientFocus',
  BusinessAcumen = 'BusinessAcumen'
}

export enum PerformanceRating {
  BelowExpectations = 'BelowExpectations',
  MeetsExpectations = 'MeetsExpectations',
  ExceedsExpectations = 'ExceedsExpectations',
  Outstanding = 'Outstanding'
}

export enum ReviewStatus {
  NotStarted = 'NotStarted',
  InProgress = 'InProgress',
  PendingApproval = 'PendingApproval',
  Completed = 'Completed'
}

export enum OnboardingTaskStatus {
  NotStarted = 'NotStarted',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Blocked = 'Blocked'
}

export enum OffboardingTaskStatus {
  NotStarted = 'NotStarted',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Blocked = 'Blocked'
}

export enum MeritIncreaseType {
  PerformanceBased = 'PerformanceBased',
  Promotion = 'Promotion',
  MarketAdjustment = 'MarketAdjustment',
  Retention = 'Retention'
}

export enum OffboardingReason {
  Resignation = 'Resignation',
  Retirement = 'Retirement',
  Termination = 'Termination',
  EndOfContract = 'EndOfContract',
  Other = 'Other'
}

export interface Department {
  id: string;
  name: string;
  managerId?: string;
  headcount: number;
  createdAt: string;
  updatedAt: string;
}

export interface Employee {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  departmentId: string;
  title: string;
  level: number;
  salary: number;
  hireDate: string;
  status: EmploymentStatus;
  createdAt: string;
  updatedAt: string;
}

export interface ApiError {
  message: string;
  detail?: string;
  statusCode: number;
  traceId?: string;
  errors?: Record<string, string[]>;
  timestamp: string;
}
