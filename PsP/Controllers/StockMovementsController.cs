using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsP.Auth;
using PsP.Contracts.StockMovements;
using PsP.Services.Interfaces;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/stock-items/{stockItemId:int}/movements")]
[Authorize]
public class StockMovementsController : ControllerBase
{
    private readonly IStockMovementService _stockMovements;

    public StockMovementsController(IStockMovementService stockMovements)
    {
        _stockMovements = stockMovements;
    }

    private ActionResult? EnsureBusinessMatchesRoute(int routeBusinessId, out int callerEmployeeId)
    {
        var jwtBizId = User.GetBusinessId();
        if (jwtBizId != routeBusinessId)
        {
            callerEmployeeId = default;
            return Forbid();
        }

        callerEmployeeId = User.GetEmployeeId();
        return null;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StockMovementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<StockMovementResponse>>> ListForStockItem(
        [FromRoute] int businessId,
        [FromRoute] int stockItemId,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId, out var callerEmployeeId);
        if (mismatch is not null) return mismatch;

        try
        {
            var result = await _stockMovements.ListAsync(
                businessId,
                stockItemId,
                callerEmployeeId,
                type,
                dateFrom,
                dateTo,
                HttpContext.RequestAborted);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return MapException(ex);
        }
    }

    [HttpGet("{movementId:int}")]
    [ProducesResponseType(typeof(StockMovementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockMovementResponse>> GetOne(
        [FromRoute] int businessId,
        [FromRoute] int stockItemId,
        [FromRoute] int movementId)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId, out var callerEmployeeId);
        if (mismatch is not null) return mismatch;

        try
        {
            var dto = await _stockMovements.GetByIdAsync(
                businessId,
                stockItemId,
                movementId,
                callerEmployeeId,
                HttpContext.RequestAborted);

            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return MapException(ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(StockMovementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockMovementResponse>> Create(
        [FromRoute] int businessId,
        [FromRoute] int stockItemId,
        [FromBody] CreateStockMovementRequest body)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId, out var callerEmployeeId);
        if (mismatch is not null) return mismatch;

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var created = await _stockMovements.CreateAsync(
                businessId,
                stockItemId,
                callerEmployeeId,
                body,
                HttpContext.RequestAborted);

            return CreatedAtAction(
                nameof(GetOne),
                new { businessId, stockItemId, movementId = created.StockMovementId },
                created);
        }
        catch (InvalidOperationException ex)
        {
            return MapException(ex);
        }
    }

    private ActionResult MapException(InvalidOperationException ex)
    {
        var msg = ex.Message;

        if (msg.Contains("not_found", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return NotFound(msg);

        if (msg.Contains("concurrency_conflict", StringComparison.OrdinalIgnoreCase))
            return Conflict(msg);

        if (msg.Contains("caller_inactive", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("caller_not_found", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        if (msg.Contains("invalid_type", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("delta_cannot_be_zero", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("unit_cost_required_for_receive", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("order_line_mismatch_stock_item", StringComparison.OrdinalIgnoreCase))
            return BadRequest(msg);

        return BadRequest(msg);
    }
}
