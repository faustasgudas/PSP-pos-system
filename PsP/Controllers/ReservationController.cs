using Microsoft.AspNetCore.Mvc;
using PsP.Contracts.Reservations;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/reservations")]
public class ReservationsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<ReservationSummaryResponse>> ListAll(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId,
        [FromQuery] string? status = null,           // "Booked" | "Cancelled" | "Completed"
        [FromQuery] DateTime? from = null,           // appointmentStart >= from
        [FromQuery] DateTime? to = null,             // appointmentStart <= to
        [FromQuery] int? employeeId = null,          // assigned staff
        [FromQuery] int? catalogItemId = null)       // reserved service
    {
        return Ok();
    }

    [HttpGet("{reservationId:int}")]
    public ActionResult<ReservationDetailResponse> GetOne(
        [FromRoute] int businessId,
        [FromRoute] int reservationId,
        [FromQuery] int callerEmployeeId)
    {
        return Ok();
    }

    [HttpPost]
    public IActionResult Create(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId,
        [FromBody] CreateReservationRequest body)
    {
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{reservationId:int}")]
    public IActionResult Update(
        [FromRoute] int businessId,
        [FromRoute] int reservationId,
        [FromQuery] int callerEmployeeId,
        [FromBody] UpdateReservationRequest body)
    {
        return Ok();
    }

    [HttpPost("{reservationId:int}/cancel")]
    public IActionResult Cancel(
        [FromRoute] int businessId,
        [FromRoute] int reservationId,
        [FromQuery] int callerEmployeeId,
        [FromBody] CancelReservationRequest body)
    {
        return Ok();
    }

    [HttpPost("{reservationId:int}/complete")]
    public IActionResult Complete(
        [FromRoute] int businessId,
        [FromRoute] int reservationId,
        [FromQuery] int callerEmployeeId)
    {
        return Ok();
    }
}