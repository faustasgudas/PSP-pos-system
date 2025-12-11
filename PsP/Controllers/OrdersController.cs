using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsP.Auth;
using PsP.Contracts.Orders;
using PsP.Services.Interfaces;

namespace PsP.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize] // All endpoints require authentication
public class OrdersController : ControllerBase
{
    private readonly IOrdersService _orders;

    public OrdersController(IOrdersService orders) => _orders = orders;

    // -------------------------------
    // GET /api/orders  (Managers+Owners)
    // -------------------------------
    [HttpGet]
    [Authorize(Roles = "Manager,Owner")]
    public async Task<ActionResult<IEnumerable<OrderSummaryResponse>>> ListAll(
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var businessId = User.GetBusinessId();
        var employeeId = User.GetEmployeeId();

        try
        {
            var result = await _orders.ListAllAsync(
                businessId, employeeId, status, from, to, HttpContext.RequestAborted);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return ForbidOrBadRequest(ex); }
    }

    // -------------------------------
    // GET /api/orders/mine
    // -------------------------------
    [HttpGet("mine")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<IEnumerable<OrderSummaryResponse>>> ListMine()
    {
        var businessId = User.GetBusinessId();
        var employeeId = User.GetEmployeeId();

        try
        {
            var result = await _orders.ListMineAsync(
                businessId, employeeId, HttpContext.RequestAborted);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return ForbidOrBadRequest(ex); }
    }

    // -------------------------------
    // GET /api/orders/{orderId}
    // -------------------------------
    [HttpGet("{orderId:int}")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<OrderDetailResponse>> GetOrder(int orderId)
    {
        var businessId = User.GetBusinessId();
        var employeeId = User.GetEmployeeId();

        try
        {
            var dto = await _orders.GetOrderAsync(
                businessId, orderId, employeeId, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // -------------------------------
    // GET /api/orders/{orderId}/lines
    // -------------------------------
    [HttpGet("{orderId:int}/lines")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<IEnumerable<OrderLineResponse>>> ListLines(int orderId)
    {
        var businessId = User.GetBusinessId();
        var employeeId = User.GetEmployeeId();

        try
        {
            var lines = await _orders.ListLinesAsync(
                businessId, orderId, employeeId, HttpContext.RequestAborted);
            return Ok(lines);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // -------------------------------
    // GET /api/orders/{orderId}/lines/{lineId}
    // -------------------------------
    [HttpGet("{orderId:int}/lines/{lineId:int}")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<OrderLineResponse>> GetLine(int orderId, int lineId)
    {
        var businessId = User.GetBusinessId();
        var employeeId = User.GetEmployeeId();

        try
        {
            var dto = await _orders.GetLineAsync(
                businessId, orderId, lineId, employeeId, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // -------------------------------
    // POST /api/orders
    // -------------------------------
    [HttpPost]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<OrderDetailResponse>> CreateOrder([FromBody] CreateOrderRequest body)
    {
        var businessId = User.GetBusinessId();
        var employeeId = User.GetEmployeeId();

        try
        {
            var dto = await _orders.CreateOrderAsync(
                businessId, employeeId, body, HttpContext.RequestAborted);

            return CreatedAtAction(nameof(GetOrder),
                new { orderId = dto.OrderId }, dto);
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    // -------------------------------
    // PUT /api/orders/{orderId}
    // -------------------------------
    [HttpPut("{orderId:int}")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<OrderDetailResponse>> UpdateOrder(
        int orderId, [FromBody] UpdateOrderRequest body)
    {
        var businessId = User.GetBusinessId();
        var employeeId = User.GetEmployeeId();

        try
        {
            var dto = await _orders.UpdateOrderAsync(
                businessId, orderId, employeeId, body, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // -------------------------------
    // POST /api/orders/{orderId}/close
    // -------------------------------
    [HttpPost("{orderId:int}/close")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<OrderDetailResponse>> CloseOrder(int orderId)
    {
        var businessId = User.GetBusinessId();
        var employeeId = User.GetEmployeeId();

        try
        {
            var dto = await _orders.CloseOrderAsync(
                businessId, orderId, employeeId, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // -------------------------------
    // POST /api/orders/{orderId}/cancel
    // -------------------------------
    [HttpPost("{orderId:int}/cancel")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<OrderDetailResponse>> CancelOrder(
        int orderId, [FromBody] CancelOrderRequest body)
    {
        var businessId = User.GetBusinessId();
        var employeeId = User.GetEmployeeId();

        try
        {
            var dto = await _orders.CancelOrderAsync(
                businessId, orderId, employeeId, body, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // -------------------------------
    // POST /api/orders/{orderId}/lines
    // -------------------------------
    [HttpPost("{orderId:int}/lines")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<OrderLineResponse>> AddLine(
        int orderId, [FromBody] AddLineRequest body)
    {
        var businessId = User.GetBusinessId();
        var employeeId = User.GetEmployeeId();

        try
        {
            var dto = await _orders.AddLineAsync(
                businessId, orderId, employeeId, body, HttpContext.RequestAborted);

            return CreatedAtAction(nameof(GetLine),
                new { orderId, lineId = dto.OrderLineId }, dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // -------------------------------
    // PUT /api/orders/{orderId}/lines/{lineId}
    // -------------------------------
    [HttpPut("{orderId:int}/lines/{lineId:int}")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<OrderLineResponse>> UpdateLine(
        int orderId, int lineId, [FromBody] UpdateLineRequest body)
    {
        var businessId = User.GetBusinessId();
        var employeeId = User.GetEmployeeId();

        try
        {
            var dto = await _orders.UpdateLineAsync(
                businessId, orderId, lineId, employeeId, body, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // -------------------------------
    // DELETE /api/orders/{orderId}/lines/{lineId}
    // -------------------------------
    [HttpDelete("{orderId:int}/lines/{lineId:int}")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<IActionResult> RemoveLine(int orderId, int lineId)
    {
        var businessId = User.GetBusinessId();
        var employeeId = User.GetEmployeeId();

        try
        {
            await _orders.RemoveLineAsync(
                businessId, orderId, lineId, employeeId, HttpContext.RequestAborted);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // -------------------------------
    // POST /api/orders/{orderId}/reopen
    // -------------------------------
    [HttpPost("{orderId:int}/reopen")]
    [Authorize(Roles = "Manager,Owner")]
    public async Task<ActionResult<OrderDetailResponse>> ReopenOrder(int orderId)
    {
        var businessId = User.GetBusinessId();
        var employeeId = User.GetEmployeeId();

        try
        {
            var dto = await _orders.ReopenOrderAsync(
                businessId, orderId, employeeId, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // -------------------------------
    // Helpers
    // -------------------------------
    private ActionResult NotFoundOrBadRequest(InvalidOperationException ex)
        => ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(ex.Message)
            : BadRequest(ex.Message);

    private ActionResult ForbidOrBadRequest(InvalidOperationException ex)
        => ex.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase)
            ? Forbid()
            : BadRequest(ex.Message);
}
