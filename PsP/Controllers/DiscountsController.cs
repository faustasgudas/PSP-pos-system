using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsP.Auth;
using PsP.Contracts.Catalog;
using PsP.Contracts.Discounts;
using PsP.Services.Interfaces;

namespace PsP.Controllers;

[ApiController]
[Route("api/discounts")]
[Authorize(Roles = "Manager,Owner")]
public class DiscountsController : ControllerBase
{
    private readonly IDiscountsService _svc;

    public DiscountsController(IDiscountsService svc) => _svc = svc;

   
    [HttpGet]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<IEnumerable<DiscountSummaryResponse>>> ListDiscounts()
    {
        var businessId = User.GetBusinessId();
        var callerId = User.GetEmployeeId();

        try
        {
            var list = await _svc.ListDiscountsAsync(businessId, callerId, HttpContext.RequestAborted);
            return Ok(list);
        }
        catch (InvalidOperationException ex) { return ForbidOrBadRequest(ex); }
    }

    [HttpGet("{discountId:int}")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<DiscountDetailResponse>> GetDiscount(int discountId)
    {
        var businessId = User.GetBusinessId();
        var callerId = User.GetEmployeeId();

        try
        {
            var dto = await _svc.GetDiscountAsync(businessId, callerId, discountId, HttpContext.RequestAborted);
            return Ok(dto);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    [HttpPost]
    public async Task<ActionResult<DiscountDetailResponse>> CreateDiscount([FromBody] CreateDiscountRequest body)
    {
        var businessId = User.GetBusinessId();
        var callerId = User.GetEmployeeId();

        try
        {
            var created = await _svc.CreateDiscountAsync(businessId, callerId, body, HttpContext.RequestAborted);

            return CreatedAtAction(
                nameof(GetDiscount),
                new { discountId = created.DiscountId },
                created
            );
        }
        catch (InvalidOperationException ex) { return ForbidOrBadRequest(ex); }
    }

    [HttpPut("{discountId:int}")]
    public async Task<ActionResult<DiscountDetailResponse>> UpdateDiscount(
        int discountId,
        [FromBody] UpdateDiscountRequest body)
    {
        var businessId = User.GetBusinessId();
        var callerId = User.GetEmployeeId();

        try
        {
            var updated = await _svc.UpdateDiscountAsync(businessId, callerId, discountId, body, HttpContext.RequestAborted);
            return Ok(updated);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    [HttpDelete("{discountId:int}")]
    public async Task<IActionResult> DeleteDiscount(int discountId)
    {
        var businessId = User.GetBusinessId();
        var callerId = User.GetEmployeeId();

        try
        {
            await _svc.DeleteDiscountAsync(businessId, callerId, discountId, HttpContext.RequestAborted);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return ForbidOrBadRequest(ex); }
    }

 
    [HttpGet("{discountId:int}/eligibilities")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<IEnumerable<DiscountEligibilityResponse>>> ListEligibilities(int discountId)
    {
        var businessId = User.GetBusinessId();
        var callerId = User.GetEmployeeId();

        try
        {
            var list = await _svc.ListEligibilitiesAsync(businessId, callerId, discountId, HttpContext.RequestAborted);
            return Ok(list);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    [HttpGet("{discountId:int}/eligible-items")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public async Task<ActionResult<IEnumerable<CatalogItemSummaryResponse>>> ListEligibleItems(int discountId)
    {
        var businessId = User.GetBusinessId();
        var callerId = User.GetEmployeeId();

        try
        {
            var list = await _svc.ListEligibleItemsAsync(businessId, callerId, discountId, HttpContext.RequestAborted);
            return Ok(list);
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

    [HttpPost("{discountId:int}/eligibilities")]
    public async Task<ActionResult<DiscountEligibilityResponse>> AddEligibility(
        int discountId,
        [FromBody] CreateDiscountEligibilityRequest body)
    {
        var businessId = User.GetBusinessId();
        var callerId = User.GetEmployeeId();

        try
        {
            var created = await _svc.AddEligibilityAsync(businessId, callerId, discountId, body, HttpContext.RequestAborted);

            return CreatedAtAction(
                nameof(ListEligibilities),
                new { discountId },
                created
            );
        }
        catch (InvalidOperationException ex) { return ForbidOrBadRequest(ex); }
    }

  
    [HttpDelete("{discountId:int}/eligibilities/{catalogItemId:int}")]
    public async Task<IActionResult> RemoveEligibility(int discountId, int catalogItemId)
    {
        var businessId = User.GetBusinessId();
        var callerId = User.GetEmployeeId();

        try
        {
            await _svc.RemoveEligibilityAsync(businessId, callerId, discountId, catalogItemId, HttpContext.RequestAborted);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFoundOrBadRequest(ex); }
    }

  
    private ActionResult NotFoundOrBadRequest(InvalidOperationException ex)
        => ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? NotFound(ex.Message)
            : BadRequest(ex.Message);

    private ActionResult ForbidOrBadRequest(InvalidOperationException ex)
        => ex.Message.StartsWith("Forbidden", StringComparison.OrdinalIgnoreCase)
            ? Forbid()
            : BadRequest(ex.Message);
}
