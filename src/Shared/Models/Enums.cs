namespace Shared.Models;

public enum EmploymentStatus
{
    Pending,
    Active,
    OnLeave,
    Terminated
}

public enum CompetencyType
{
    TechnicalExpertise,
    Leadership,
    Communication,
    ProblemSolving,
    Collaboration,
    Innovation,
    ClientFocus,
    BusinessAcumen
}

public enum PerformanceRating
{
    BelowExpectations,
    MeetsExpectations,
    ExceedsExpectations,
    Outstanding
}

public enum ReviewStatus
{
    NotStarted,
    InProgress,
    PendingApproval,
    Completed
}

public enum OnboardingTaskStatus
{
    NotStarted,
    InProgress,
    Completed,
    Blocked
}

public enum OffboardingTaskStatus
{
    NotStarted,
    InProgress,
    Completed,
    Blocked
}

public enum MeritIncreaseType
{
    PerformanceBased,
    Promotion,
    MarketAdjustment,
    Retention
}

public enum OffboardingReason
{
    Resignation,
    Retirement,
    Termination,
    EndOfContract,
    Other
}
