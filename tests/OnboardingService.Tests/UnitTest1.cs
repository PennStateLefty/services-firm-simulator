using OnboardingService.Models;
using Shared.Models;

namespace OnboardingService.Tests;

public class OnboardingCaseModelTests
{
    [Fact]
    public void OnboardingCase_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var onboardingCase = new OnboardingCase();

        // Assert
        Assert.NotNull(onboardingCase.Id);
        Assert.NotEqual(Guid.Empty.ToString(), onboardingCase.Id);
        Assert.Equal(string.Empty, onboardingCase.EmployeeId);
        Assert.NotNull(onboardingCase.Tasks);
        Assert.Empty(onboardingCase.Tasks);
        Assert.True(onboardingCase.CreatedAt <= DateTime.UtcNow);
        Assert.True(onboardingCase.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void OnboardingCase_ShouldSupportNullableFields()
    {
        // Arrange
        var onboardingCase = new OnboardingCase
        {
            EmployeeId = "emp-123",
            StartDate = DateTime.UtcNow,
            TargetCompletionDate = null,
            ActualCompletionDate = null,
            Status = OnboardingTaskStatus.InProgress,
            Notes = null
        };

        // Assert
        Assert.Null(onboardingCase.TargetCompletionDate);
        Assert.Null(onboardingCase.ActualCompletionDate);
        Assert.Null(onboardingCase.Notes);
    }

    [Fact]
    public void OnboardingCase_ShouldUseSharedOnboardingTaskStatusEnum()
    {
        // Arrange
        var onboardingCase = new OnboardingCase
        {
            Status = OnboardingTaskStatus.NotStarted
        };

        // Assert
        Assert.Equal(OnboardingTaskStatus.NotStarted, onboardingCase.Status);
    }

    [Fact]
    public void OnboardingTask_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var task = new OnboardingTask();

        // Assert
        Assert.NotNull(task.Id);
        Assert.NotEqual(Guid.Empty.ToString(), task.Id);
        Assert.Equal(string.Empty, task.Description);
        Assert.Equal(string.Empty, task.AssignedTo);
    }

    [Fact]
    public void OnboardingCase_ShouldSupportTasksCollection()
    {
        // Arrange
        var onboardingCase = new OnboardingCase
        {
            EmployeeId = "emp-123",
            StartDate = DateTime.UtcNow,
            Status = OnboardingTaskStatus.InProgress
        };

        var task1 = new OnboardingTask
        {
            Description = "Complete paperwork",
            TaskType = OnboardingTaskType.Paperwork,
            Status = OnboardingTaskStatus.NotStarted
        };

        var task2 = new OnboardingTask
        {
            Description = "Equipment setup",
            TaskType = OnboardingTaskType.Equipment,
            Status = OnboardingTaskStatus.InProgress
        };

        // Act
        onboardingCase.Tasks.Add(task1);
        onboardingCase.Tasks.Add(task2);

        // Assert
        Assert.Equal(2, onboardingCase.Tasks.Count);
        Assert.Contains(task1, onboardingCase.Tasks);
        Assert.Contains(task2, onboardingCase.Tasks);
    }
}
