using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OnboardingService.Models;
using OnboardingService.Services;
using Shared.Models;
using Task = System.Threading.Tasks.Task;

namespace OnboardingService.Tests;

public class TaskTemplateServiceTests
{
    private readonly Mock<ILogger<TaskTemplateService>> _mockLogger;

    public TaskTemplateServiceTests()
    {
        _mockLogger = new Mock<ILogger<TaskTemplateService>>();
    }

    private static List<TaskTemplate> CreateDefaultTemplates()
    {
        return new List<TaskTemplate>
        {
            new TaskTemplate
            {
                Description = "Setup workstation",
                TaskType = OnboardingTaskType.Equipment,
                Order = 1,
                DueDateOffsetDays = 7
            },
            new TaskTemplate
            {
                Description = "HR paperwork",
                TaskType = OnboardingTaskType.Paperwork,
                Order = 2,
                DueDateOffsetDays = 14
            },
            new TaskTemplate
            {
                Description = "IT account",
                TaskType = OnboardingTaskType.Access,
                Order = 3,
                DueDateOffsetDays = 21
            },
            new TaskTemplate
            {
                Description = "Security badge",
                TaskType = OnboardingTaskType.Access,
                Order = 4,
                DueDateOffsetDays = 28
            },
            new TaskTemplate
            {
                Description = "Benefits enrollment",
                TaskType = OnboardingTaskType.Paperwork,
                Order = 5,
                DueDateOffsetDays = 30
            }
        };
    }

    [Fact]
    public void GenerateTasksFromTemplates_WithValidTemplates_GeneratesCorrectNumberOfTasks()
    {
        // Arrange
        var templates = CreateDefaultTemplates();
        var mockOptions = new Mock<IOptions<List<TaskTemplate>>>();
        mockOptions.Setup(o => o.Value).Returns(templates);
        var service = new TaskTemplateService(mockOptions.Object, _mockLogger.Object);
        var startDate = DateTime.UtcNow;

        // Act
        var tasks = service.GenerateTasksFromTemplates(startDate);

        // Assert
        Assert.Equal(5, tasks.Count);
    }

    [Fact]
    public void GenerateTasksFromTemplates_WithValidTemplates_SetsCorrectDescriptions()
    {
        // Arrange
        var templates = CreateDefaultTemplates();
        var mockOptions = new Mock<IOptions<List<TaskTemplate>>>();
        mockOptions.Setup(o => o.Value).Returns(templates);
        var service = new TaskTemplateService(mockOptions.Object, _mockLogger.Object);
        var startDate = DateTime.UtcNow;

        // Act
        var tasks = service.GenerateTasksFromTemplates(startDate);

        // Assert
        Assert.Equal("Setup workstation", tasks[0].Description);
        Assert.Equal("HR paperwork", tasks[1].Description);
        Assert.Equal("IT account", tasks[2].Description);
        Assert.Equal("Security badge", tasks[3].Description);
        Assert.Equal("Benefits enrollment", tasks[4].Description);
    }

    [Fact]
    public void GenerateTasksFromTemplates_WithValidTemplates_SetsCorrectTaskTypes()
    {
        // Arrange
        var templates = CreateDefaultTemplates();
        var mockOptions = new Mock<IOptions<List<TaskTemplate>>>();
        mockOptions.Setup(o => o.Value).Returns(templates);
        var service = new TaskTemplateService(mockOptions.Object, _mockLogger.Object);
        var startDate = DateTime.UtcNow;

        // Act
        var tasks = service.GenerateTasksFromTemplates(startDate);

        // Assert
        Assert.Equal(OnboardingTaskType.Equipment, tasks[0].TaskType);
        Assert.Equal(OnboardingTaskType.Paperwork, tasks[1].TaskType);
        Assert.Equal(OnboardingTaskType.Access, tasks[2].TaskType);
        Assert.Equal(OnboardingTaskType.Access, tasks[3].TaskType);
        Assert.Equal(OnboardingTaskType.Paperwork, tasks[4].TaskType);
    }

    [Fact]
    public void GenerateTasksFromTemplates_WithValidTemplates_SetsCorrectDueDates()
    {
        // Arrange
        var templates = CreateDefaultTemplates();
        var mockOptions = new Mock<IOptions<List<TaskTemplate>>>();
        mockOptions.Setup(o => o.Value).Returns(templates);
        var service = new TaskTemplateService(mockOptions.Object, _mockLogger.Object);
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var tasks = service.GenerateTasksFromTemplates(startDate);

        // Assert
        Assert.Equal(startDate.AddDays(7), tasks[0].DueDate);
        Assert.Equal(startDate.AddDays(14), tasks[1].DueDate);
        Assert.Equal(startDate.AddDays(21), tasks[2].DueDate);
        Assert.Equal(startDate.AddDays(28), tasks[3].DueDate);
        Assert.Equal(startDate.AddDays(30), tasks[4].DueDate);
    }

    [Fact]
    public void GenerateTasksFromTemplates_WithValidTemplates_SetsStatusToNotStarted()
    {
        // Arrange
        var templates = CreateDefaultTemplates();
        var mockOptions = new Mock<IOptions<List<TaskTemplate>>>();
        mockOptions.Setup(o => o.Value).Returns(templates);
        var service = new TaskTemplateService(mockOptions.Object, _mockLogger.Object);
        var startDate = DateTime.UtcNow;

        // Act
        var tasks = service.GenerateTasksFromTemplates(startDate);

        // Assert
        Assert.All(tasks, task => Assert.Equal(OnboardingTaskStatus.NotStarted, task.Status));
    }

    [Fact]
    public void GenerateTasksFromTemplates_WithValidTemplates_AssignsUniqueIds()
    {
        // Arrange
        var templates = CreateDefaultTemplates();
        var mockOptions = new Mock<IOptions<List<TaskTemplate>>>();
        mockOptions.Setup(o => o.Value).Returns(templates);
        var service = new TaskTemplateService(mockOptions.Object, _mockLogger.Object);
        var startDate = DateTime.UtcNow;

        // Act
        var tasks = service.GenerateTasksFromTemplates(startDate);

        // Assert
        var uniqueIds = tasks.Select(t => t.Id).Distinct();
        Assert.Equal(tasks.Count, uniqueIds.Count());
        Assert.All(tasks, task => Assert.False(string.IsNullOrEmpty(task.Id)));
    }

    [Fact]
    public void GenerateTasksFromTemplates_WithValidTemplates_OrdersTasksByOrderProperty()
    {
        // Arrange
        var templates = new List<TaskTemplate>
        {
            new TaskTemplate
            {
                Description = "Third task",
                TaskType = OnboardingTaskType.Other,
                Order = 3,
                DueDateOffsetDays = 5
            },
            new TaskTemplate
            {
                Description = "First task",
                TaskType = OnboardingTaskType.Other,
                Order = 1,
                DueDateOffsetDays = 10
            },
            new TaskTemplate
            {
                Description = "Second task",
                TaskType = OnboardingTaskType.Other,
                Order = 2,
                DueDateOffsetDays = 15
            }
        };
        
        var mockOptions = new Mock<IOptions<List<TaskTemplate>>>();
        mockOptions.Setup(o => o.Value).Returns(templates);
        var service = new TaskTemplateService(mockOptions.Object, _mockLogger.Object);
        var startDate = DateTime.UtcNow;

        // Act
        var tasks = service.GenerateTasksFromTemplates(startDate);

        // Assert
        Assert.Equal("First task", tasks[0].Description);
        Assert.Equal("Second task", tasks[1].Description);
        Assert.Equal("Third task", tasks[2].Description);
    }

    [Fact]
    public void GenerateTasksFromTemplates_WithEmptyTemplates_ReturnsEmptyList()
    {
        // Arrange
        var templates = new List<TaskTemplate>();
        var mockOptions = new Mock<IOptions<List<TaskTemplate>>>();
        mockOptions.Setup(o => o.Value).Returns(templates);
        var service = new TaskTemplateService(mockOptions.Object, _mockLogger.Object);
        var startDate = DateTime.UtcNow;

        // Act
        var tasks = service.GenerateTasksFromTemplates(startDate);

        // Assert
        Assert.Empty(tasks);
    }

    [Fact]
    public void GenerateTasksFromTemplates_WithNullTemplates_ReturnsEmptyList()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<List<TaskTemplate>>>();
        mockOptions.Setup(o => o.Value).Returns((List<TaskTemplate>?)null);
        var service = new TaskTemplateService(mockOptions.Object, _mockLogger.Object);
        var startDate = DateTime.UtcNow;

        // Act
        var tasks = service.GenerateTasksFromTemplates(startDate);

        // Assert
        Assert.Empty(tasks);
    }

    [Fact]
    public void GenerateTasksFromTemplates_WithValidTemplates_InitializesAssignedToAsEmpty()
    {
        // Arrange
        var templates = CreateDefaultTemplates();
        var mockOptions = new Mock<IOptions<List<TaskTemplate>>>();
        mockOptions.Setup(o => o.Value).Returns(templates);
        var service = new TaskTemplateService(mockOptions.Object, _mockLogger.Object);
        var startDate = DateTime.UtcNow;

        // Act
        var tasks = service.GenerateTasksFromTemplates(startDate);

        // Assert
        Assert.All(tasks, task => Assert.Equal(string.Empty, task.AssignedTo));
    }

    [Fact]
    public void GenerateTasksFromTemplates_WithStartDate_CalculatesDueDatesFromStartDate()
    {
        // Arrange
        var templates = new List<TaskTemplate>
        {
            new TaskTemplate
            {
                Description = "Task 1",
                TaskType = OnboardingTaskType.Other,
                Order = 1,
                DueDateOffsetDays = 10
            }
        };
        
        var mockOptions = new Mock<IOptions<List<TaskTemplate>>>();
        mockOptions.Setup(o => o.Value).Returns(templates);
        var service = new TaskTemplateService(mockOptions.Object, _mockLogger.Object);
        var startDate = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var tasks = service.GenerateTasksFromTemplates(startDate);

        // Assert
        Assert.Single(tasks);
        Assert.Equal(new DateTime(2024, 6, 25, 12, 0, 0, DateTimeKind.Utc), tasks[0].DueDate);
    }
}
