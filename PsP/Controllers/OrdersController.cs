using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsP.Auth;
using PsP.Contracts.Orders;
using PsP.Services.Interfaces;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrdersService _orders;

    public OrdersController(IOrdersService orders) => _orders = orders;

    private ActionResult? EnsureBusinessMatchesRoute(int routeBusinessId)
    {
        var jwtBizId = User.GetBusinessId();
        if (jwtBizId != routeBusinessId)
            return Forbid();

        return null;
    }

    // Managers/Owners: list ALL. Staff rejected in service.
    [HttpGet("ListAllOrders")]
    public async Task<ActionResult<IEnumerable<OrderSummaryResponse>>> ListAll(
        [FromRoute] int businessId,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();
        var callerRole       = User.GetRole();

        try
        {
            var result = await _orders.ListAllAsync(
                businessId,
                callerEmployeeId,
                callerRole,
                status,
                from,
                to,
                HttpContext.RequestAborted);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return ForbidOrBadRequest(ex);
        }
    }

    // Caller’s own orders (staff: only Open)
    [HttpGet("ListMyOrders")]
    public async Task<ActionResult<IEnumerable<OrderSummaryResponse>>> ListMine(
        [FromRoute] int businessId)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();
        var callerRole       = User.GetRole();

        try
        {
            var result = await _orders.ListMineAsync(
                businessId,
                callerEmployeeId,
                callerRole,
                HttpContext.RequestAborted);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return ForbidOrBadRequest(ex);
        }
    }

    // Get one order (Staff only if theirs; Managers/Owners allowed)
    [HttpGet("GetOrder/{orderId:int}")]
    public async Task<ActionResult<OrderDetailResponse>> GetOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();
        var callerRole       = User.GetRole();

        try
        {
            var dto = await _orders.GetOrderAsync(
                businessId,
                orderId,
                callerEmployeeId,
                callerRole,
                HttpContext.RequestAborted);

            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundOrBadRequest(ex);
        }
    }

    // LIST lines for an order
    [HttpGet("GetOrder/{orderId:int}/ListOrderLines")]
    public async Task<ActionResult<IEnumerable<OrderLineResponse>>> ListLines(
        [FromRoute] int businessId,
        [FromRoute] int orderId)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();
        var callerRole       = User.GetRole();

        try
        {
            var result = await _orders.ListLinesAsync(
                businessId,
                orderId,
                callerEmployeeId,
                callerRole,
                HttpContext.RequestAborted);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundOrBadRequest(ex);
        }
    }

    // GET a single line
    [HttpGet("GetOrder/{orderId:int}/OrderLine/{orderLineId:int}")]
    public async Task<ActionResult<OrderLineResponse>> GetLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromRoute] int orderLineId)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();
        var callerRole       = User.GetRole();

        try
        {
            var dto = await _orders.GetLineAsync(
                businessId,
                orderId,
                orderLineId,
                callerEmployeeId,
                callerRole,
                HttpContext.RequestAborted);

            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundOrBadRequest(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<OrderDetailResponse>> CreateOrder(
        [FromRoute] int businessId,
        [FromBody] CreateOrderRequest body)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();
        var callerRole       = User.GetRole();

        try
        {
            var dto = await _orders.CreateOrderAsync(
                businessId,
                callerEmployeeId,
                callerRole,
                body,
                HttpContext.RequestAborted);

            return CreatedAtAction(
                nameof(GetOrder),
                new { businessId, orderId = dto.OrderId },
                dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Update an OPEN order
    [HttpPut("{orderId:int}")]
    public async Task<ActionResult<OrderDetailResponse>> UpdateOpenOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromBody] UpdateOrderRequest body)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();
        var callerRole       = User.GetRole();

        try
        {
            var dto = await _orders.UpdateOrderAsync(
                businessId,
                orderId,
                callerEmployeeId,
                callerRole,
                body,
                HttpContext.RequestAborted);

            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundOrBadRequest(ex);
        }
    }

    [HttpPost("{orderId:int}/close")]
    public async Task<ActionResult<OrderDetailResponse>> CloseOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();
        var callerRole       = User.GetRole();

        try
        {
            var dto = await _orders.CloseOrderAsync(
                businessId,
                orderId,
                callerEmployeeId,
                callerRole,
                HttpContext.RequestAborted);

            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundOrBadRequest(ex);
        }
    }

    [HttpPost("{orderId:int}/cancel")]
    public async Task<ActionResult<OrderDetailResponse>> CancelOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromBody] CancelOrderRequest body)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();
        var callerRole       = User.GetRole();

        try
        {
            var dto = await _orders.CancelOrderAsync(
                businessId,
                orderId,
                callerEmployeeId,
                callerRole,
                body,
                HttpContext.RequestAborted);

            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundOrBadRequest(ex);
        }
    }

    [HttpPost("{orderId:int}/lines")]
    public async Task<ActionResult<OrderLineResponse>> AddLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromBody] AddLineRequest body)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();
        var callerRole       = User.GetRole();

        try
        {
            var dto = await _orders.AddLineAsync(
                businessId,
                orderId,
                callerEmployeeId,
                callerRole,
                body,
                HttpContext.RequestAborted);

            return CreatedAtAction(
                nameof(GetLine),
                new { businessId, orderId, orderLineId = dto.OrderLineId },
                dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundOrBadRequest(ex);
        }
    }

    [HttpPut("{orderId:int}/lines/{orderLineId:int}")]
    public async Task<ActionResult<OrderLineResponse>> UpdateLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromRoute] int orderLineId,
        [FromBody] UpdateLineRequest body)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();
        var callerRole       = User.GetRole();

        try
        {
            var dto = await _orders.UpdateLineAsync(
                businessId,
                orderId,
                orderLineId,
                callerEmployeeId,
                callerRole,
                body,
                HttpContext.RequestAborted);

            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundOrBadRequest(ex);
        }
    }

    [HttpDelete("{orderId:int}/lines/{orderLineId:int}")]
    public async Task<IActionResult> RemoveLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromRoute] int orderLineId)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();
        var callerRole       = User.GetRole();

        try
        {
            await _orders.RemoveLineAsync(
                businessId,
                orderId,
                orderLineId,
                callerEmployeeId,
                callerRole,
                HttpContext.RequestAborted);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundOrBadRequest(ex);
        }
    }

    // --- small helpers to map common service exceptions ---
    private ActionResult NotFoundOrBadRequest(InvalidOperationException ex)
        => ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(ex.Message)
            : BadRequest(ex.Message);

    private ActionResult ForbidOrBadRequest(InvalidOperationException ex)
        => ex.Message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase)
            ? Forbid()
            : BadRequest(ex.Message);
}
