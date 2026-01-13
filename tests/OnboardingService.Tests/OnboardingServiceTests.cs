using OnboardingService.Infrastructure;
using OnboardingService.Models;
using OnboardingService.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;

namespace OnboardingService.Tests;

public class OnboardingServiceTests
{
    private readonly Mock<IDaprStateStore> _mockStateStore;
    private readonly Mock<ILogger<OnboardingServiceImpl>> _mockLogger;
    private readonly IOnboardingService _onboardingService;

    public OnboardingServiceTests()
    {
        _mockStateStore = new Mock<IDaprStateStore>();
        _mockLogger = new Mock<ILogger<OnboardingServiceImpl>>();
        _onboardingService = new OnboardingServiceImpl(
            _mockStateStore.Object,
            _mockLogger.Object);
    }

    private static OnboardingCase CreateTestOnboardingCase(
        string id = "test-id",
        string employeeId = "emp-123",
        OnboardingTaskStatus status = OnboardingTaskStatus.NotStarted)
    {
        return new OnboardingCase
        {
            Id = id,
            EmployeeId = employeeId,
            StartDate = DateTime.UtcNow,
            TargetCompletionDate = DateTime.UtcNow.AddDays(30),
            Status = status,
            Tasks = new List<OnboardingTask>
            {
                new OnboardingTask
                {
                    Id = "task-1",
                    Description = "Complete paperwork",
                    TaskType = OnboardingTaskType.Paperwork,
                    AssignedTo = "hr-manager",
                    Status = OnboardingTaskStatus.NotStarted
                }
            },
            Notes = "Test onboarding case",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async System.Threading.Tasks.Task GetStateAsync_ExistingCase_ReturnsOnboardingCase()
    {
        // Arrange
        var caseId = "test-id";
        var expectedCase = CreateTestOnboardingCase(id: caseId);

        _mockStateStore.Setup(x => x.GetStateAsync<OnboardingCase>(
            $"onboarding-case:{caseId}", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCase);

        // Act
        var result = await _onboardingService.GetStateAsync(caseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(caseId, result.Id);
        Assert.Equal("emp-123", result.EmployeeId);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetStateAsync_NonExistingCase_ReturnsNull()
    {
        // Arrange
        var caseId = "non-existing-id";
        _mockStateStore.Setup(x => x.GetStateAsync<OnboardingCase>(
            $"onboarding-case:{caseId}", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingCase?)null);

        // Act
        var result = await _onboardingService.GetStateAsync(caseId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetStateAsync_ExceptionThrown_RethrowsException()
    {
        // Arrange
        var caseId = "error-id";
        _mockStateStore.Setup(x => x.GetStateAsync<OnboardingCase>(
            $"onboarding-case:{caseId}", 
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("State store error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _onboardingService.GetStateAsync(caseId));
    }

    [Fact]
    public async System.Threading.Tasks.Task SaveStateAsync_NewCase_CreatesCase()
    {
        // Arrange
        var newCase = CreateTestOnboardingCase();
        
        _mockStateStore.Setup(x => x.GetStateAsync<OnboardingCase>(
            $"onboarding-case:{newCase.Id}", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingCase?)null);

        _mockStateStore.Setup(x => x.SaveStateAsync(
            $"onboarding-case:{newCase.Id}",
            It.IsAny<OnboardingCase>(),
            It.IsAny<CancellationToken>()))
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        // Act
        var result = await _onboardingService.SaveStateAsync(newCase);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newCase.Id, result.Id);
        _mockStateStore.Verify(x => x.SaveStateAsync(
            $"onboarding-case:{newCase.Id}",
            It.IsAny<OnboardingCase>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task SaveStateAsync_ExistingCase_UpdatesCase()
    {
        // Arrange
        var existingCase = CreateTestOnboardingCase();
        var originalCreatedAt = existingCase.CreatedAt;
        
        _mockStateStore.Setup(x => x.GetStateAsync<OnboardingCase>(
            $"onboarding-case:{existingCase.Id}", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCase);

        _mockStateStore.Setup(x => x.SaveStateAsync(
            $"onboarding-case:{existingCase.Id}",
            It.IsAny<OnboardingCase>(),
            It.IsAny<CancellationToken>()))
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        // Update the case
        existingCase.Notes = "Updated notes";

        // Act
        var result = await _onboardingService.SaveStateAsync(existingCase);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated notes", result.Notes);
        Assert.Equal(originalCreatedAt, result.CreatedAt); // CreatedAt preserved
        _mockStateStore.Verify(x => x.SaveStateAsync(
            $"onboarding-case:{existingCase.Id}",
            It.IsAny<OnboardingCase>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task SaveStateAsync_WithTasks_SavesTaskCollection()
    {
        // Arrange
        var newCase = CreateTestOnboardingCase();
        newCase.Tasks.Add(new OnboardingTask
        {
            Id = "task-2",
            Description = "Complete training",
            TaskType = OnboardingTaskType.Training,
            AssignedTo = "trainer",
            Status = OnboardingTaskStatus.NotStarted
        });
        
        _mockStateStore.Setup(x => x.GetStateAsync<OnboardingCase>(
            $"onboarding-case:{newCase.Id}", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingCase?)null);

        _mockStateStore.Setup(x => x.SaveStateAsync(
            $"onboarding-case:{newCase.Id}",
            It.IsAny<OnboardingCase>(),
            It.IsAny<CancellationToken>()))
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        // Act
        var result = await _onboardingService.SaveStateAsync(newCase);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Tasks.Count);
        _mockStateStore.Verify(x => x.SaveStateAsync(
            $"onboarding-case:{newCase.Id}",
            It.Is<OnboardingCase>(c => c.Tasks.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task SaveStateAsync_NullCase_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _onboardingService.SaveStateAsync(null!));
    }

    [Fact]
    public async System.Threading.Tasks.Task SaveStateAsync_ExceptionThrown_RethrowsException()
    {
        // Arrange
        var newCase = CreateTestOnboardingCase();
        
        _mockStateStore.Setup(x => x.GetStateAsync<OnboardingCase>(
            $"onboarding-case:{newCase.Id}", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingCase?)null);

        _mockStateStore.Setup(x => x.SaveStateAsync(
            $"onboarding-case:{newCase.Id}",
            It.IsAny<OnboardingCase>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("State store error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _onboardingService.SaveStateAsync(newCase));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteStateAsync_ExistingCase_DeletesCase()
    {
        // Arrange
        var caseId = "test-id";
        var existingCase = CreateTestOnboardingCase(id: caseId);

        _mockStateStore.Setup(x => x.GetStateAsync<OnboardingCase>(
            $"onboarding-case:{caseId}", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCase);

        _mockStateStore.Setup(x => x.DeleteStateAsync(
            $"onboarding-case:{caseId}",
            It.IsAny<CancellationToken>()))
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        // Act
        await _onboardingService.DeleteStateAsync(caseId);

        // Assert
        _mockStateStore.Verify(x => x.DeleteStateAsync(
            $"onboarding-case:{caseId}",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteStateAsync_NonExistingCase_ThrowsKeyNotFoundException()
    {
        // Arrange
        var caseId = "non-existing-id";

        _mockStateStore.Setup(x => x.GetStateAsync<OnboardingCase>(
            $"onboarding-case:{caseId}", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingCase?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _onboardingService.DeleteStateAsync(caseId));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteStateAsync_ExceptionThrown_RethrowsException()
    {
        // Arrange
        var caseId = "error-id";
        var existingCase = CreateTestOnboardingCase(id: caseId);

        _mockStateStore.Setup(x => x.GetStateAsync<OnboardingCase>(
            $"onboarding-case:{caseId}", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCase);

        _mockStateStore.Setup(x => x.DeleteStateAsync(
            $"onboarding-case:{caseId}",
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("State store error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _onboardingService.DeleteStateAsync(caseId));
    }

    [Fact]
    public async System.Threading.Tasks.Task QueryStateAsync_WithFilter_ReturnsMatchingCases()
    {
        // Arrange
        var filter = "{\"filter\":{\"EQ\":{\"employeeId\":\"emp-123\"}}}";
        var expectedCases = new List<OnboardingCase>
        {
            CreateTestOnboardingCase(id: "case-1", employeeId: "emp-123"),
            CreateTestOnboardingCase(id: "case-2", employeeId: "emp-123")
        };

        _mockStateStore.Setup(x => x.QueryStateAsync<OnboardingCase>(
            filter,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCases);

        // Act
        var result = await _onboardingService.QueryStateAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async System.Threading.Tasks.Task QueryStateAsync_NoMatches_ReturnsEmptyCollection()
    {
        // Arrange
        var filter = "{\"filter\":{\"EQ\":{\"employeeId\":\"non-existing\"}}}";

        _mockStateStore.Setup(x => x.QueryStateAsync<OnboardingCase>(
            filter,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OnboardingCase>());

        // Act
        var result = await _onboardingService.QueryStateAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task QueryStateAsync_ExceptionThrown_RethrowsException()
    {
        // Arrange
        var filter = "{}";

        _mockStateStore.Setup(x => x.QueryStateAsync<OnboardingCase>(
            filter,
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("State store error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _onboardingService.QueryStateAsync(filter));
    }

    [Fact]
    public async System.Threading.Tasks.Task SaveStateAsync_UpdatesTimestamp()
    {
        // Arrange
        var newCase = CreateTestOnboardingCase();
        var originalUpdatedAt = newCase.UpdatedAt;
        
        // Simulate a delay to ensure timestamp changes
        await System.Threading.Tasks.Task.Delay(10);
        
        _mockStateStore.Setup(x => x.GetStateAsync<OnboardingCase>(
            $"onboarding-case:{newCase.Id}", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingCase?)null);

        _mockStateStore.Setup(x => x.SaveStateAsync(
            $"onboarding-case:{newCase.Id}",
            It.IsAny<OnboardingCase>(),
            It.IsAny<CancellationToken>()))
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        // Act
        var result = await _onboardingService.SaveStateAsync(newCase);

        // Assert
        Assert.True(result.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public async System.Threading.Tasks.Task SaveStateAsync_UsesCorrectKeyPattern()
    {
        // Arrange
        var caseId = "custom-case-id";
        var newCase = CreateTestOnboardingCase(id: caseId);
        
        _mockStateStore.Setup(x => x.GetStateAsync<OnboardingCase>(
            $"onboarding-case:{caseId}", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingCase?)null);

        _mockStateStore.Setup(x => x.SaveStateAsync(
            $"onboarding-case:{caseId}",
            It.IsAny<OnboardingCase>(),
            It.IsAny<CancellationToken>()))
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        // Act
        await _onboardingService.SaveStateAsync(newCase);

        // Assert
        _mockStateStore.Verify(x => x.SaveStateAsync(
            $"onboarding-case:{caseId}",
            It.IsAny<OnboardingCase>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
