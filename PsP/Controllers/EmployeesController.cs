using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsP.Contracts.Employees;
using PsP.Mappings;
using PsP.Services.Interfaces;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employees;

    public EmployeesController(IEmployeeService employees)
    {
        _employees = employees;
    }

    private int GetCallerEmployeeId()
    {
        var idClaim = User.FindFirst("employeeId");
        if (idClaim is null)
            throw new UnauthorizedAccessException("Missing employeeId claim");

        return int.Parse(idClaim.Value);
    }

    private int GetBusinessIdFromToken()
    {
        var bizClaim = User.FindFirst("businessId");
        if (bizClaim is null)
            throw new UnauthorizedAccessException("Missing businessId claim");

        return int.Parse(bizClaim.Value);
    }

   
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeSummaryResponse>>> ListAll(
        [FromRoute] int businessId,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var tokenBizId = GetBusinessIdFromToken();
            if (tokenBizId != businessId)
                return Forbid(); 

            var callerEmployeeId = GetCallerEmployeeId();

            var list = await _employees.GetAllAsync(businessId, callerEmployeeId, role, status);
            var resp = list.Select(e => e.ToSummaryResponse()).ToList();
            return Ok(resp);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex) when (ex.Message == "forbidden")
        {
            return Forbid();
        }
        catch (InvalidOperationException ex) when (ex.Message == "caller_not_found_or_wrong_business")
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromRoute] int businessId,
        [FromBody] CreateEmployeeRequest body)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var tokenBizId = GetBusinessIdFromToken();
            if (tokenBizId != businessId)
                return Forbid();

            var callerEmployeeId = GetCallerEmployeeId();

            var entity = body.ToNewEntity(businessId);

            var created = await _employees.CreateAsync(businessId, callerEmployeeId, entity);

            var resp = created.ToSummaryResponse();

            return CreatedAtAction(
                nameof(ListAll),
                new { businessId },
                resp);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex) when (ex.Message == "forbidden")
        {
            return Forbid();
        }
    }

    [HttpPut("{employeeId:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int businessId,
        [FromRoute] int employeeId,
        [FromBody] UpdateEmployeeRequest body)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var tokenBizId = GetBusinessIdFromToken();
            if (tokenBizId != businessId)
                return Forbid();

            var callerEmployeeId = GetCallerEmployeeId();

            var existing = await _employees.GetByIdAsync(businessId, employeeId, callerEmployeeId);
            if (existing is null)
                return NotFound();

            body.ApplyUpdate(existing);

            var updated = await _employees.UpdateAsync(businessId, employeeId, callerEmployeeId, existing);
            if (updated is null) return NotFound();

            return Ok(updated.ToSummaryResponse());
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex) when (ex.Message == "forbidden")
        {
            return Forbid();
        }
    }

    [HttpPost("{employeeId:int}/deactivate")]
    public async Task<IActionResult> Deactivate(
        [FromRoute] int businessId,
        [FromRoute] int employeeId,
        [FromBody] DeactivateEmployeeRequest body)
    {
        try
        {
            var tokenBizId = GetBusinessIdFromToken();
            if (tokenBizId != businessId)
                return Forbid();

            var callerEmployeeId = GetCallerEmployeeId();

            var updated = await _employees.DeactivateAsync(
                businessId,
                employeeId,
                callerEmployeeId,
                body.Reason);

            if (updated is null)
                return NotFound();

            return Ok(updated.ToSummaryResponse());
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex) when (ex.Message == "forbidden")
        {
            return Forbid();
        }
    }
}
