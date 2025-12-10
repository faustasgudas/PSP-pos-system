using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsP.Auth;
using PsP.Contracts.Reservations;
using PsP.Services.Interfaces;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;

    public ReservationsController(IReservationService reservations)
    {
        _reservations = reservations;
    }

    private ActionResult? EnsureBusinessMatchesRoute(int routeBusinessId)
    {
        var jwtBizId = User.GetBusinessId();
        if (jwtBizId != routeBusinessId)
            return Forbid();
        return null;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReservationSummaryResponse>>> ListAll(
        [FromRoute] int businessId,
        [FromQuery] string? status = null,
        [FromQuery(Name = "dateFrom")] DateTime? dateFrom = null,
        [FromQuery(Name = "dateTo")] DateTime? dateTo = null,
        [FromQuery] int? employeeId = null,
        [FromQuery] int? catalogItemId = null)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();

        try
        {
            var list = await _reservations.ListAsync(
                businessId,
                callerEmployeeId,
                status,
                dateFrom,
                dateTo,
                employeeId,
                catalogItemId,
                HttpContext.RequestAborted);

            return Ok(list);
        }
        catch (InvalidOperationException ex) { return MapException(ex); }
    }

    [HttpGet("{reservationId:int}")]
    public async Task<ActionResult<ReservationDetailResponse>> GetOne(
        [FromRoute] int businessId,
        [FromRoute] int reservationId)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();

        try
        {
            var dto = await _reservations.GetAsync(
                businessId,
                reservationId,
                callerEmployeeId,
                HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return MapException(ex); }
    }

    [HttpPost]
    public async Task<ActionResult<ReservationDetailResponse>> Create(
        [FromRoute] int businessId,
        [FromBody] CreateReservationRequest body)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();

        try
        {
            var created = await _reservations.CreateAsync(
                businessId,
                callerEmployeeId,
                body,
                HttpContext.RequestAborted);

            return CreatedAtAction(nameof(GetOne),
                new { businessId, reservationId = created.ReservationId },
                created);
        }
        catch (InvalidOperationException ex) { return MapException(ex); }
    }

    [HttpPatch("{reservationId:int}")]
    public async Task<ActionResult<ReservationDetailResponse>> Update(
        [FromRoute] int businessId,
        [FromRoute] int reservationId,
        [FromBody] UpdateReservationRequest body)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();

        try
        {
            var updated = await _reservations.UpdateAsync(
                businessId,
                reservationId,
                callerEmployeeId,
                body,
                HttpContext.RequestAborted);
            return Ok(updated);
        }
        catch (InvalidOperationException ex) { return MapException(ex); }
    }

    [HttpDelete("{reservationId:int}")]
    public async Task<ActionResult<ReservationDetailResponse>> Cancel(
        [FromRoute] int businessId,
        [FromRoute] int reservationId)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();

        try
        {
            var dto = await _reservations.CancelAsync(
                businessId,
                reservationId,
                callerEmployeeId,
                HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return MapException(ex); }
    }

    private ActionResult MapException(InvalidOperationException ex)
    {
        if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return NotFound(ex.Message);
        if (ex.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase))
            return Forbid();
        return BadRequest(ex.Message);
    }
}