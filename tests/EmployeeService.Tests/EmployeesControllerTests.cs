using EmployeeService.Controllers;
using EmployeeService.Models;
using EmployeeService.Services;
using EmployeeService.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace EmployeeService.Tests;

public class EmployeesControllerTests
{
    private readonly Mock<IEmployeeService> _mockEmployeeService;
    private readonly Mock<IDepartmentService> _mockDepartmentService;
    private readonly Mock<ILogger<EmployeesController>> _mockLogger;
    private readonly EmployeesController _controller;

    public EmployeesControllerTests()
    {
        _mockEmployeeService = new Mock<IEmployeeService>();
        _mockDepartmentService = new Mock<IDepartmentService>();
        _mockLogger = new Mock<ILogger<EmployeesController>>();
        _controller = new EmployeesController(
            _mockEmployeeService.Object,
            _mockDepartmentService.Object,
            _mockLogger.Object
        );
        
        // Set up HttpContext for TraceIdentifier
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task CreateEmployee_ValidRequest_Returns201Created()
    {
        // Arrange
        var departmentId = "dept-123";
        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = departmentId,
            Title = "Software Engineer",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Pending
        };

        var department = new Department
        {
            Id = departmentId,
            Name = "Engineering"
        };

        var createdEmployee = new Employee
        {
            Id = "emp-456",
            EmployeeNumber = "EMP2026000001",
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DepartmentId = request.DepartmentId,
            Title = request.Title,
            Level = request.Level,
            Salary = request.Salary,
            HireDate = request.HireDate,
            Status = request.Status
        };

        _mockDepartmentService.Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _mockEmployeeService.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEmployee);

        // Act
        var result = await _controller.CreateEmployee(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(EmployeesController.GetEmployee), createdResult.ActionName);
        Assert.NotNull(createdResult.RouteValues);
        Assert.Equal("emp-456", createdResult.RouteValues["employeeId"]);
        
        var returnedEmployee = Assert.IsType<Employee>(createdResult.Value);
        Assert.Equal(createdEmployee.Id, returnedEmployee.Id);
        Assert.Equal(createdEmployee.Email, returnedEmployee.Email);
    }

    [Fact]
    public async Task CreateEmployee_DepartmentNotFound_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "non-existent-dept",
            Title = "Software Engineer",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Pending
        };

        _mockDepartmentService.Setup(x => x.GetByIdAsync(request.DepartmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        var result = await _controller.CreateEmployee(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
        
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Equal(400, errorResponse.StatusCode);
        Assert.NotNull(errorResponse.Errors);
        Assert.Contains("DepartmentId", errorResponse.Errors.Keys);
    }

    [Fact]
    public async Task CreateEmployee_DuplicateEmail_Returns409Conflict()
    {
        // Arrange
        var departmentId = "dept-123";
        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@example.com",
            DepartmentId = departmentId,
            Title = "Software Engineer",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Pending
        };

        var department = new Department { Id = departmentId, Name = "Engineering" };
        
        _mockDepartmentService.Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _mockEmployeeService.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailAlreadyExistsException(request.Email));

        // Act
        var result = await _controller.CreateEmployee(request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(409, conflictResult.StatusCode);
        
        var errorResponse = Assert.IsType<ErrorResponse>(conflictResult.Value);
        Assert.Equal(409, errorResponse.StatusCode);
        Assert.Contains(request.Email, errorResponse.Message);
    }

    [Fact]
    public async Task CreateEmployee_ValidationException_Returns400BadRequest()
    {
        // Arrange
        var departmentId = "dept-123";
        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "invalid-email",
            DepartmentId = departmentId,
            Title = "Software Engineer",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Pending
        };

        var department = new Department { Id = departmentId, Name = "Engineering" };
        
        _mockDepartmentService.Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _mockEmployeeService.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Invalid email format"));

        // Act
        var result = await _controller.CreateEmployee(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
        
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Equal(400, errorResponse.StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_UnexpectedException_Returns500InternalServerError()
    {
        // Arrange
        var departmentId = "dept-123";
        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = departmentId,
            Title = "Software Engineer",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Pending
        };

        var department = new Department { Id = departmentId, Name = "Engineering" };
        
        _mockDepartmentService.Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _mockEmployeeService.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.CreateEmployee(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
        Assert.Equal(500, errorResponse.StatusCode);
    }

    [Fact]
    public async Task GetEmployee_ExistingEmployee_Returns200Ok()
    {
        // Arrange
        var employeeId = "emp-123";
        var employee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP2026000001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "dept-123",
            Status = EmploymentStatus.Active
        };

        _mockEmployeeService.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _controller.GetEmployee(employeeId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        
        var returnedEmployee = Assert.IsType<Employee>(okResult.Value);
        Assert.Equal(employeeId, returnedEmployee.Id);
        Assert.Equal("john.doe@example.com", returnedEmployee.Email);
    }

    [Fact]
    public async Task GetEmployee_NonExistingEmployee_Returns404NotFound()
    {
        // Arrange
        var employeeId = "non-existent-emp";
        
        _mockEmployeeService.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _controller.GetEmployee(employeeId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
        
        var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal(404, errorResponse.StatusCode);
        Assert.Contains("Employee", errorResponse.Message);
    }

    [Fact]
    public async Task GetEmployee_ExceptionThrown_Returns500InternalServerError()
    {
        // Arrange
        var employeeId = "emp-123";
        
        _mockEmployeeService.Setup(x => x.GetByIdAsync(employeeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetEmployee(employeeId, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
        Assert.Equal(500, errorResponse.StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_ReturnsLocationHeader()
    {
        // Arrange
        var departmentId = "dept-123";
        var request = new CreateEmployeeRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            DepartmentId = departmentId,
            Title = "Senior Engineer",
            Level = 6,
            Salary = 120000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Pending
        };

        var department = new Department { Id = departmentId, Name = "Engineering" };
        
        var createdEmployee = new Employee
        {
            Id = "emp-789",
            EmployeeNumber = "EMP2026000002",
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DepartmentId = request.DepartmentId,
            Status = request.Status
        };

        _mockDepartmentService.Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _mockEmployeeService.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEmployee);

        // Act
        var result = await _controller.CreateEmployee(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(createdResult.RouteValues);
        Assert.Equal("emp-789", createdResult.RouteValues["employeeId"]);
        Assert.Equal(nameof(EmployeesController.GetEmployee), createdResult.ActionName);
    }

    [Fact]
    public async Task GetEmployees_DefaultParameters_Returns200Ok()
    {
        // Arrange
        var response = new EmployeeListResponse
        {
            Employees = new List<EmployeeSummary>
            {
                new EmployeeSummary
                {
                    Id = "emp-1",
                    EmployeeNumber = "EMP2026000001",
                    FullName = "John Doe",
                    Email = "john.doe@example.com",
                    JobTitle = "Software Engineer",
                    Department = "Engineering",
                    Status = "Active"
                }
            },
            Pagination = new PaginationMetadata
            {
                Page = 1,
                PageSize = 50,
                TotalCount = 1,
                TotalPages = 1
            }
        };

        _mockEmployeeService.Setup(x => x.GetEmployeesAsync(1, 50, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetEmployees(1, 50, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        
        var returnedResponse = Assert.IsType<EmployeeListResponse>(okResult.Value);
        Assert.Single(returnedResponse.Employees);
        Assert.Equal(1, returnedResponse.Pagination.TotalCount);
    }

    [Fact]
    public async Task GetEmployees_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var response = new EmployeeListResponse
        {
            Employees = new List<EmployeeSummary>
            {
                new EmployeeSummary
                {
                    Id = "emp-21",
                    EmployeeNumber = "EMP2026000021",
                    FullName = "Jane Smith",
                    Email = "jane.smith@example.com",
                    JobTitle = "Senior Engineer",
                    Department = "Engineering",
                    Status = "Active"
                }
            },
            Pagination = new PaginationMetadata
            {
                Page = 2,
                PageSize = 20,
                TotalCount = 25,
                TotalPages = 2
            }
        };

        _mockEmployeeService.Setup(x => x.GetEmployeesAsync(2, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetEmployees(2, 20, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResponse = Assert.IsType<EmployeeListResponse>(okResult.Value);
        Assert.Equal(2, returnedResponse.Pagination.Page);
        Assert.Equal(20, returnedResponse.Pagination.PageSize);
        Assert.Equal(25, returnedResponse.Pagination.TotalCount);
    }

    [Fact]
    public async Task GetEmployees_WithStatusFilter_CallsServiceWithMappedStatus()
    {
        // Arrange
        var response = new EmployeeListResponse
        {
            Employees = new List<EmployeeSummary>(),
            Pagination = new PaginationMetadata
            {
                Page = 1,
                PageSize = 50,
                TotalCount = 0,
                TotalPages = 0
            }
        };

        _mockEmployeeService.Setup(x => x.GetEmployeesAsync(1, 50, "Pending", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetEmployees(1, 50, "Onboarding", null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockEmployeeService.Verify(
            x => x.GetEmployeesAsync(1, 50, "Pending", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEmployees_WithDepartmentFilter_ResolvesAndFilters()
    {
        // Arrange
        var departments = new List<Department>
        {
            new Department { Id = "dept-123", Name = "Engineering" },
            new Department { Id = "dept-456", Name = "Sales" }
        };

        var response = new EmployeeListResponse
        {
            Employees = new List<EmployeeSummary>
            {
                new EmployeeSummary
                {
                    Id = "emp-1",
                    EmployeeNumber = "EMP2026000001",
                    FullName = "John Doe",
                    Email = "john.doe@example.com",
                    JobTitle = "Software Engineer",
                    Department = "Engineering",
                    Status = "Active"
                }
            },
            Pagination = new PaginationMetadata
            {
                Page = 1,
                PageSize = 50,
                TotalCount = 1,
                TotalPages = 1
            }
        };

        _mockDepartmentService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(departments);

        _mockEmployeeService.Setup(x => x.GetEmployeesAsync(1, 50, null, "dept-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetEmployees(1, 50, null, "Engineering", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _mockEmployeeService.Verify(
            x => x.GetEmployeesAsync(1, 50, null, "dept-123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEmployees_WithNonExistentDepartment_ReturnsEmptyList()
    {
        // Arrange
        var departments = new List<Department>
        {
            new Department { Id = "dept-123", Name = "Engineering" }
        };

        _mockDepartmentService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(departments);

        // Act
        var result = await _controller.GetEmployees(1, 50, null, "NonExistent", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResponse = Assert.IsType<EmployeeListResponse>(okResult.Value);
        Assert.Empty(returnedResponse.Employees);
        Assert.Equal(0, returnedResponse.Pagination.TotalCount);
        
        // Service should not be called when department doesn't exist
        _mockEmployeeService.Verify(
            x => x.GetEmployeesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetEmployees_InvalidPage_Returns400BadRequest(int page)
    {
        // Act
        var result = await _controller.GetEmployees(page, 50, null, null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
        
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Contains("Page", errorResponse.Detail ?? string.Empty);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetEmployees_InvalidPageSize_Returns400BadRequest(int pageSize)
    {
        // Act
        var result = await _controller.GetEmployees(1, pageSize, null, null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
        
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Contains("PageSize", errorResponse.Detail ?? string.Empty);
    }

    [Theory]
    [InlineData("InvalidStatus")]
    [InlineData("Pending")]
    public async Task GetEmployees_InvalidStatus_Returns400BadRequest(string status)
    {
        // Act
        var result = await _controller.GetEmployees(1, 50, status, null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
        
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Contains("Status", errorResponse.Detail ?? string.Empty);
    }

    [Fact]
    public async Task GetEmployees_ExceptionThrown_Returns500InternalServerError()
    {
        // Arrange
        _mockEmployeeService.Setup(x => x.GetEmployeesAsync(
            It.IsAny<int>(), 
            It.IsAny<int>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetEmployees(1, 50, null, null, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
        Assert.Equal(500, errorResponse.StatusCode);
    }
}
