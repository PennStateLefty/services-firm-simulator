using Microsoft.AspNetCore.Mvc;
using EmployeeService.Models;
using EmployeeService.Services;
using EmployeeService.Exceptions;
using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace EmployeeService.Controllers;

[ApiController]
[Route("v1/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(
        IEmployeeService employeeService,
        IDepartmentService departmentService,
        ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
        _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<ActionResult<Employee>> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating employee with email: {Email}", request.Email);

            // Validate required fields
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );
                return BadRequest(ErrorResponse.ValidationError(errors, HttpContext.TraceIdentifier));
            }

            // Validate department exists
            var department = await _departmentService.GetByIdAsync(request.DepartmentId, cancellationToken);
            if (department == null)
            {
                _logger.LogWarning("Department not found: {DepartmentId}", request.DepartmentId);
                return BadRequest(ErrorResponse.ValidationError(
                    new Dictionary<string, string[]>
                    {
                        ["DepartmentId"] = new[] { $"Department with ID '{request.DepartmentId}' does not exist" }
                    },
                    HttpContext.TraceIdentifier
                ));
            }

            // Create employee
            var employee = await _employeeService.CreateAsync(request, cancellationToken);

            _logger.LogInformation("Employee created successfully: {EmployeeId}", employee.Id);

            // Return 201 Created with Location header
            return CreatedAtAction(
                nameof(GetEmployee),
                new { employeeId = employee.Id },
                employee
            );
        }
        catch (EmailAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "Email already exists: {Email}", ex.Email);
            return Conflict(ErrorResponse.Conflict(ex.Message, HttpContext.TraceIdentifier));
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error during employee creation");
            return BadRequest(ErrorResponse.Create(400, "Validation failed", ex.Message, HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee");
            return StatusCode(500, ErrorResponse.InternalServerError(ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpGet("{employeeId}")]
    public async Task<ActionResult<Employee>> GetEmployee(string employeeId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting employee: {EmployeeId}", employeeId);
            
            var employee = await _employeeService.GetByIdAsync(employeeId, cancellationToken);
            
            if (employee == null)
            {
                return NotFound(ErrorResponse.NotFound("Employee", HttpContext.TraceIdentifier));
            }

            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee: {EmployeeId}", employeeId);
            return StatusCode(500, ErrorResponse.InternalServerError(ex.Message, HttpContext.TraceIdentifier));
        }
    }
}
