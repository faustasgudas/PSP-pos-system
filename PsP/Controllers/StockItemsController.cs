using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsP.Auth;
using PsP.Contracts.StockItems;
using PsP.Services.Interfaces;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/stock-items")]
[Authorize]
public class StockItemsController : ControllerBase
{
    private readonly IStockItemService _stockItems;

    public StockItemsController(IStockItemService stockItems)
    {
        _stockItems = stockItems;
    }

    /// <summary>
    /// Patikrina, kad businessId iš JWT sutaptų su route ir grąžina employeeId iš JWT.
    /// </summary>
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

    // ===== LIST =====

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StockItemSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<StockItemSummaryResponse>>> ListAll(
        [FromRoute] int businessId,
        [FromQuery] int? catalogItemId = null)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId, out var callerEmployeeId);
        if (mismatch is not null) return mismatch;

        try
        {
            var list = await _stockItems.ListAsync(
                businessId,
                callerEmployeeId,
                catalogItemId,
                HttpContext.RequestAborted);

            return Ok(list);
        }
        catch (InvalidOperationException ex)
        {
            return MapException(ex);
        }
    }

    // ===== GET ONE =====

    [HttpGet("{stockItemId:int}")]
    [ProducesResponseType(typeof(StockItemDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StockItemDetailResponse>> GetOne(
        [FromRoute] int businessId,
        [FromRoute] int stockItemId)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId, out var callerEmployeeId);
        if (mismatch is not null) return mismatch;

        try
        {
            var dto = await _stockItems.GetOneAsync(
                businessId,
                stockItemId,
                callerEmployeeId,
                HttpContext.RequestAborted);

            if (dto is null) return NotFound();
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return MapException(ex);
        }
    }

    // ===== CREATE =====

    [HttpPost]
    [ProducesResponseType(typeof(StockItemDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockItemDetailResponse>> Create(
        [FromRoute] int businessId,
        [FromBody] CreateStockItemRequest body)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId, out var callerEmployeeId);
        if (mismatch is not null) return mismatch;

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var created = await _stockItems.CreateAsync(
                businessId,
                callerEmployeeId,
                body,
                HttpContext.RequestAborted);

            return CreatedAtAction(
                nameof(GetOne),
                new { businessId, stockItemId = created.StockItemId },
                created);
        }
        catch (InvalidOperationException ex)
        {
            return MapException(ex);
        }
    }

    // ===== UPDATE =====

    [HttpPut("{stockItemId:int}")]
    [ProducesResponseType(typeof(StockItemDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockItemDetailResponse>> Update(
        [FromRoute] int businessId,
        [FromRoute] int stockItemId,
        [FromBody] UpdateStockItemRequest body)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId, out var callerEmployeeId);
        if (mismatch is not null) return mismatch;

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var updated = await _stockItems.UpdateAsync(
                businessId,
                stockItemId,
                callerEmployeeId,
                body,
                HttpContext.RequestAborted);

            if (updated is null) return NotFound();
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return MapException(ex);
        }
    }

    // ===== COMMON ERROR MAPPING =====

    private ActionResult MapException(InvalidOperationException ex)
    {
        var msg = ex.Message;

        if (msg.Contains("forbidden", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        if (msg.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return NotFound(msg);

        if (msg.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            return Conflict(msg);

        if (msg.Contains("caller_inactive", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("caller_not_found_or_wrong_business", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        return BadRequest(msg);
    }
}
