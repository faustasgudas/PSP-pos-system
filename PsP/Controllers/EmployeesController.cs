using Microsoft.AspNetCore.Mvc;
using PsP.Contracts.Employees;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/employees")]
public class EmployeesController : ControllerBase
{
    // Managers/Owners list; staff blocked in service
    [HttpGet]
    public ActionResult<IEnumerable<EmployeeSummaryResponse>> ListAll(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId,
        [FromQuery] string? role = null,     // "Owner" | "Manager" | "Staff"
        [FromQuery] string? status = null)   // "Active" | "OnLeave" | "Terminated"
    {
        return Ok();
    }
    

    [HttpPost]
    public IActionResult Create(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId,
        [FromBody] CreateEmployeeRequest body)
    {
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{employeeId:int}")]
    public IActionResult Update(
        [FromRoute] int businessId,
        [FromRoute] int employeeId,
        [FromQuery] int callerEmployeeId,
        [FromBody] UpdateEmployeeRequest body)
    {
        return Ok();
    }

    // Action to change status without PATCH
    [HttpPost("{employeeId:int}/deactivate")]
    public IActionResult Deactivate(
        [FromRoute] int businessId,
        [FromRoute] int employeeId,
        [FromQuery] int callerEmployeeId,
        [FromBody] DeactivateEmployeeRequest body)
    {
        return Ok();
    }
}