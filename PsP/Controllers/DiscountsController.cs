using Microsoft.AspNetCore.Mvc;
using PsP.Contracts.Catalog;
using PsP.Contracts.Discounts;
using PsP.Services.Interfaces;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/discounts")]
public class DiscountsController : ControllerBase
{
    private readonly IDiscountsService _svc;

    public DiscountsController(IDiscountsService svc) => _svc = svc;

    // ===== DISCOUNTS =====

    /// <summary>List all discounts for a business.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DiscountSummaryResponse>>> ListDiscounts(
        [FromRoute] int businessId,
        [FromQuery] int callerId)
    {
        try
        {
            var list = await _svc.ListDiscountsAsync(businessId, callerId, HttpContext.RequestAborted);
            return Ok(list);
        }
        catch (InvalidOperationException ex) { return ForbidOrBadRequest(ex); }
    }

    /// <summary>Get one discount with details (incl. eligibilities).</summary>
    [HttpGet("{discountId:int}")]
    public async Task<ActionResult<DiscountDetailResponse>> GetDiscount(
        [FromRoute] int businessId,
        [FromQuery] int callerId,
        [FromRoute] int discountId)
    {
        try
        {
            var dto = await _svc.GetDiscountAsync(businessId, callerId, discountId, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    /// <summary>Create a new discount.</summary>
    [HttpPost]
    public async Task<ActionResult<DiscountDetailResponse>> CreateDiscount(
        [FromRoute] int businessId,
        [FromQuery] int callerId,
        [FromBody] CreateDiscountRequest body)
    {
        try
        {
            var created = await _svc.CreateDiscountAsync(businessId, callerId, body, HttpContext.RequestAborted);
            return CreatedAtAction(nameof(GetDiscount),
                new { businessId, discountId = created.DiscountId, callerId },
                created);
        }
        catch (InvalidOperationException ex) { return ForbidOrBadRequest(ex); }
    }

    /// <summary>Update an existing discount (partial via request fields).</summary>
    [HttpPut("{discountId:int}")]
    public async Task<ActionResult<DiscountDetailResponse>> UpdateDiscount(
        [FromRoute] int businessId,
        [FromQuery] int callerId,
        [FromRoute] int discountId,
        [FromBody] UpdateDiscountRequest body)
    {
        try
        {
            var updated = await _svc.UpdateDiscountAsync(businessId, callerId, discountId, body, HttpContext.RequestAborted);
            return Ok(updated);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    /// <summary>Delete a discount.</summary>
    [HttpDelete("{discountId:int}")]
    public async Task<IActionResult> DeleteDiscount(
        [FromRoute] int businessId,
        [FromQuery] int callerId,
        [FromRoute] int discountId)
    {
        try
        {
            await _svc.DeleteDiscountAsync(businessId, callerId, discountId, HttpContext.RequestAborted);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return ForbidOrBadRequest(ex); }
    }

    // ===== ELIGIBILITIES (scoped to a discount) =====

    /// <summary>List eligibilities (catalog items) for a discount.</summary>
    [HttpGet("{discountId:int}/eligibilities")]
    public async Task<ActionResult<IEnumerable<DiscountEligibilityResponse>>> ListEligibilities(
        [FromRoute] int businessId,
        [FromQuery] int callerId,
        [FromRoute] int discountId)
    {
        try
        {
            var list = await _svc.ListEligibilitiesAsync(businessId, callerId, discountId, HttpContext.RequestAborted);
            return Ok(list);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    [HttpGet("{discountId:int}/eligible-items")]
    public async Task<ActionResult<IEnumerable<CatalogItemSummaryResponse>>> ListEligibleItems(
        [FromRoute] int businessId,
        [FromQuery] int callerId,
        [FromRoute] int discountId)
    {
        try
        {
            var list = await _svc.ListEligibleItemsAsync(businessId, callerId, discountId, HttpContext.RequestAborted);
            return Ok(list);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }
    
    
    
    
    /// <summary>Add an eligibility row for this discount.</summary>
    [HttpPost("{discountId:int}/eligibilities")]
    public async Task<ActionResult<DiscountEligibilityResponse>> AddEligibility(
        [FromRoute] int businessId,
        [FromQuery] int callerId,
        [FromRoute] int discountId,
        [FromBody] CreateDiscountEligibilityRequest body)
    {
        try
        {
            var created = await _svc.AddEligibilityAsync(businessId, callerId, discountId, body, HttpContext.RequestAborted);
            return CreatedAtAction(nameof(ListEligibilities),
                new { businessId, discountId, callerId },
                created);
        }
        catch (InvalidOperationException ex) { return ForbidOrBadRequest(ex); }
    }

    /// <summary>Remove an eligibility for specific CatalogItem.</summary>
    [HttpDelete("{discountId:int}/eligibilities/{catalogItemId:int}")]
    public async Task<IActionResult> RemoveEligibility(
        [FromRoute] int businessId,
        [FromQuery] int callerId,
        [FromRoute] int discountId,
        [FromRoute] int catalogItemId)
    {
        try
        {
            await _svc.RemoveEligibilityAsync(businessId, callerId, discountId, catalogItemId, HttpContext.RequestAborted);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    // --- common translation helpers ---
    private ActionResult NotFoundOrBadRequest(InvalidOperationException ex)
        => ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(ex.Message)
            : BadRequest(ex.Message);

    private ActionResult ForbidOrBadRequest(InvalidOperationException ex)
        => ex.Message.StartsWith("Forbidden", StringComparison.OrdinalIgnoreCase)
            ? Forbid()
            : BadRequest(ex.Message);
}