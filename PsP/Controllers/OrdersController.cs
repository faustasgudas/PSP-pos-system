using Microsoft.AspNetCore.Mvc;
using PsP.Contracts.Orders;
using PsP.Services.Interfaces;


namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrdersService _orders;

    public OrdersController(IOrdersService orders) => _orders = orders;

    // Managers/Owners: list ALL. Staff rejected in service.
    [HttpGet("ListAllOrders")]
    public async Task<ActionResult<IEnumerable<OrderSummaryResponse>>> ListAll(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var result = await _orders.ListAllAsync(businessId, callerEmployeeId, status, from, to, HttpContext.RequestAborted);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return ForbidOrBadRequest(ex); }
    }

    // Caller’s own orders (staff: only Open)
    [HttpGet("ListMyOrders")]
    public async Task<ActionResult<IEnumerable<OrderSummaryResponse>>> ListMine(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId)
    {
        try
        {
            var result = await _orders.ListMineAsync(businessId, callerEmployeeId, HttpContext.RequestAborted);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return ForbidOrBadRequest(ex); }
    }

    // Get one order (Staff only if theirs; Managers/Owners allowed)
    [HttpGet("GetOrder/{orderId:int}")]
    public async Task<ActionResult<OrderDetailResponse>> GetOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId)
    {
        try
        {
            var dto = await _orders.GetOrderAsync(businessId, orderId, callerEmployeeId, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // LIST lines for an order
    [HttpGet("GetOrder/{orderId:int}/ListOrderLines")]
    public async Task<ActionResult<IEnumerable<OrderLineResponse>>> ListLines(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId)
    {
        try
        {
            var result = await _orders.ListLinesAsync(businessId, orderId, callerEmployeeId, HttpContext.RequestAborted);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // GET a single line
    [HttpGet("GetOrder/{orderId:int}/OrderLine/{orderLineId:int}")]
    public async Task<ActionResult<OrderLineResponse>> GetLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromRoute] int orderLineId,
        [FromQuery] int callerEmployeeId)
    {
        try
        {
            var dto = await _orders.GetLineAsync(businessId, orderId, orderLineId, callerEmployeeId, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    [HttpPost]
    public async Task<ActionResult<OrderDetailResponse>> CreateOrder(
        [FromRoute] int businessId,
        [FromBody] CreateOrderRequest body)
    {
        try
        {
            var dto = await _orders.CreateOrderAsync(businessId, body, HttpContext.RequestAborted);
            return CreatedAtAction(nameof(GetOrder),
                new { businessId, orderId = dto.OrderId, callerEmployeeId = body.EmployeeId }, dto);
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    // Update an OPEN order
    [HttpPut("{orderId:int}")]
    public async Task<ActionResult<OrderDetailResponse>> UpdateOpenOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId,
        [FromBody] UpdateOrderRequest body)
    {
        try
        {
            var dto = await _orders.UpdateOrderAsync(businessId, orderId, callerEmployeeId, body, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    [HttpPost("{orderId:int}/close")]
    public async Task<ActionResult<OrderDetailResponse>> CloseOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId)
    {
        try
        {
            var dto = await _orders.CloseOrderAsync(businessId, orderId, callerEmployeeId, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    [HttpPost("{orderId:int}/cancel")]
    public async Task<ActionResult<OrderDetailResponse>> CancelOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId,
        [FromBody] CancelOrderRequest body)
    {
        try
        {
            var dto = await _orders.CancelOrderAsync(businessId, orderId, callerEmployeeId, body, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    [HttpPost("{orderId:int}/lines")]
    public async Task<ActionResult<OrderLineResponse>> AddLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId,
        [FromBody] AddLineRequest body)
    {
        try
        {
            var dto = await _orders.AddLineAsync(businessId, orderId, callerEmployeeId, body, HttpContext.RequestAborted);
            return CreatedAtAction(nameof(GetLine),
                new { businessId, orderId, orderLineId = dto.OrderLineId, callerEmployeeId }, dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    [HttpPut("{orderId:int}/lines/{orderLineId:int}")]
    public async Task<ActionResult<OrderLineResponse>> UpdateLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromRoute] int orderLineId,
        [FromQuery] int callerEmployeeId,
        [FromBody] UpdateLineRequest body)
    {
        try
        {
            var dto = await _orders.UpdateLineAsync(businessId, orderId, orderLineId, callerEmployeeId, body, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    [HttpDelete("{orderId:int}/lines/{orderLineId:int}")]
    public async Task<IActionResult> RemoveLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromRoute] int orderLineId,
        [FromQuery] int callerEmployeeId)
    {
        try
        {
            await _orders.RemoveLineAsync(businessId, orderId, orderLineId, callerEmployeeId, HttpContext.RequestAborted);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // --- small helpers to map common service exceptions ---
    private ActionResult NotFoundOrBadRequest(InvalidOperationException ex)
        => ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? NotFound(ex.Message) : BadRequest(ex.Message);

    private ActionResult ForbidOrBadRequest(InvalidOperationException ex)
        => ex.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase) ? Forbid() : BadRequest(ex.Message);
}

