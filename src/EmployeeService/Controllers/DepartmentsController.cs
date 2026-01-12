using Microsoft.AspNetCore.Mvc;
using EmployeeService.Models;
using EmployeeService.Services;
using Shared.Models;

namespace EmployeeService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(IDepartmentService departmentService, ILogger<DepartmentsController> logger)
    {
        _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Department>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var departments = await _departmentService.GetAllAsync(cancellationToken);
            return Ok(departments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all departments");
            return StatusCode(500, ErrorResponse.InternalServerError(ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Department>> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var department = await _departmentService.GetByIdAsync(id, cancellationToken);
            
            if (department == null)
            {
                return NotFound(ErrorResponse.NotFound("Department", HttpContext.TraceIdentifier));
            }

            return Ok(department);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting department by ID: {Id}", id);
            return StatusCode(500, ErrorResponse.InternalServerError(ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpPost]
    public async Task<ActionResult<Department>> Create([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(ErrorResponse.ValidationError(
                    new Dictionary<string, string[]> { ["Name"] = new[] { "Department name is required" } },
                    HttpContext.TraceIdentifier
                ));
            }

            var department = await _departmentService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = department.Id }, department);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department");
            return StatusCode(500, ErrorResponse.InternalServerError(ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Department>> Update(string id, [FromBody] UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var department = await _departmentService.UpdateAsync(id, request, cancellationToken);
            return Ok(department);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ErrorResponse.NotFound("Department", HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department: {Id}", id);
            return StatusCode(500, ErrorResponse.InternalServerError(ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _departmentService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ErrorResponse.NotFound("Department", HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting department: {Id}", id);
            return StatusCode(500, ErrorResponse.InternalServerError(ex.Message, HttpContext.TraceIdentifier));
        }
    }
}
