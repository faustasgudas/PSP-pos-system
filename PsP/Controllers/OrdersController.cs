using Microsoft.AspNetCore.Mvc;
using PsP.Contracts.Orders;


namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/orders")]
public class OrdersController : ControllerBase
{
    // Managers/Owners: list ALL. Staff: should be rejected in service.
    [HttpGet("ListAllOrders")]
    public ActionResult<IEnumerable<OrderSummaryResponse>
    
    > ListAll(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId,
        [FromQuery] string? status = null, // Open | Closed | Cancelled | Refunded
        [FromQuery] DateTime? from = null, // createdAt >= from
        [FromQuery] DateTime? to = null) // createdAt <= to
    {
        return Ok();
    }

    // Caller’s own orders (for Staff typically only open ones; enforce in service)
    [HttpGet("ListMyOrders")]
    public ActionResult<IEnumerable<OrderSummaryResponse>> ListMine(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId
    ) // default: workers see only open
    {
        return Ok();
    }

    // Get one order (Staff only if they own it; Managers/Owners allowed)
    [HttpGet("GetOrder/{orderId:int}")]
    public ActionResult<OrderDetailResponse> GetOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId)
    {
        return Ok();
    }

    // LIST all lines for an order
// GET /api/businesses/{businessId}/orders/{orderId}/lines
    [HttpGet("GetOrder/{orderId:int}/ListOrderLines")]
    public ActionResult<IEnumerable<OrderLineResponse>> ListLines(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId)
    {
        return Ok();
    }

    // GET a single line
// GET /api/businesses/{businessId}/orders/{orderId}/lines/{orderLineId}

    [HttpGet("GetOrder/{orderId:int}/OrderLine/{orderLineId:int}")]
    public ActionResult<OrderLineResponse> GetLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromRoute] int orderLineId,
        [FromQuery] int callerEmployeeId)
    {
        return NotFound();
    }




    [HttpPost]
    public IActionResult CreateOrder(
        [FromRoute] int businessId,
        [FromBody] CreateOrderRequest body)
    {
        return StatusCode(StatusCodes.Status201Created);
    }

    // Update an OPEN order (move table, set/cancel status, set order-level discountId, tip)
    [HttpPut("{orderId:int}")]
    public IActionResult UpdateOpenOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId,
        [FromBody] UpdateOrderRequest body)
    {
        return Ok();
    }


    [HttpPost("{orderId:int}/close")]
    public IActionResult CloseOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId)
    {
        return Ok();
    }

    // Cancel an OPEN order (action)
    [HttpPost("{orderId:int}/cancel")]
    public IActionResult CancelOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId,
        [FromBody] CancelOrderRequest body)
    {
        return Ok();
    }


    [HttpPost("{orderId:int}/lines")]
    public IActionResult AddLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId,
        [FromBody] AddLineRequest body)
    {
        return StatusCode(StatusCodes.Status201Created);
    }

    // Update a line on an OPEN order
    [HttpPut("{orderId:int}/lines/{orderLineId:int}")]
    public IActionResult UpdateLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromRoute] int orderLineId,
        [FromQuery] int callerEmployeeId,
        [FromBody] UpdateLineRequest body)
    {
        return Ok();
    }

    [HttpDelete("{orderId:int}/lines/{orderLineId:int}")]
    public IActionResult RemoveLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromRoute] int orderLineId,
        [FromQuery] int callerEmployeeId)
    {
        return NoContent();
    }
}

