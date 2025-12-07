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

    private ActionResult? EnsureBusinessMatchesRoute(int routeBusinessId)
    {
        var jwtBizId = User.GetBusinessId();
        if (jwtBizId != routeBusinessId)
            return Forbid();
        return null;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StockMovementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<StockMovementResponse>>> ListForStockItem(
        [FromRoute] int businessId,
        [FromRoute] int stockItemId,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? type = null) // "Receive" | "Sale" | "RefundReturn" | "Waste" | "Adjust"
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();

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
        catch (InvalidOperationException ex) { return MapException(ex); }
    }

    [HttpGet("{movementId:int}")]
    [ProducesResponseType(typeof(StockMovementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockMovementResponse>> GetOne(
        [FromRoute] int businessId,
        [FromRoute] int stockItemId,
        [FromRoute] int movementId)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();

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
        catch (InvalidOperationException ex) { return MapException(ex); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(StockMovementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockMovementResponse>> Create(
        [FromRoute] int businessId,
        [FromRoute] int stockItemId,
        [FromBody] CreateStockMovementRequest body)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var callerEmployeeId = User.GetEmployeeId();

        try
        {
            var created = await _stockMovements.CreateAsync(
                businessId,
                stockItemId,
                callerEmployeeId,
                body,
                HttpContext.RequestAborted);

            return CreatedAtAction(nameof(GetOne),
                new { businessId, stockItemId, movementId = created.StockMovementId },
                created);
        }
        catch (InvalidOperationException ex) { return MapException(ex); }
    }

    private ActionResult MapException(InvalidOperationException ex)
    {
        if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return NotFound(ex.Message);
        if (ex.Message.Contains("Concurrency conflict", StringComparison.OrdinalIgnoreCase))
            return Conflict(ex.Message);
        return BadRequest(ex.Message);
    }
}

