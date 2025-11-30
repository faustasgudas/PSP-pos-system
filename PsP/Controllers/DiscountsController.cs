using Microsoft.AspNetCore.Mvc;
using PsP.Contracts.Discounts;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/discounts")]
public class DiscountsController : ControllerBase
{
    // ===== DISCOUNTS =====

    /// <summary>List all discounts for a business.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<DiscountSummaryResponse>> ListDiscounts(
        [FromRoute] int businessId)
    {
        // return Ok(mappedResults);
        return Ok(Array.Empty<DiscountSummaryResponse>());
    }

    /// <summary>Get one discount with details (incl. eligibilities).</summary>
    [HttpGet("{discountId:int}")]
    public ActionResult<DiscountDetailResponse> GetDiscount(
        [FromRoute] int businessId,
        [FromRoute] int discountId)
    {
        // return Ok(mappedResult);
        return Ok();
    }

    /// <summary>Create a new discount.</summary>
    [HttpPost]
    public IActionResult CreateDiscount(
        [FromRoute] int businessId,
        [FromBody] CreateDiscountRequest body)
    {
        // var created = ...
        // return CreatedAtAction(nameof(GetDiscount), new { businessId, discountId = created.DiscountId }, createdResponse);
        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>Update an existing discount (partial via request fields).</summary>
    [HttpPut("{discountId:int}")]
    public IActionResult UpdateDiscount(
        [FromRoute] int businessId,
        [FromRoute] int discountId,
        [FromBody] UpdateDiscountRequest body)
    {
        // update and return Ok(updatedResponse);
        return Ok();
    }

    /// <summary>Delete a discount.</summary>
    [HttpDelete("{discountId:int}")]
    public IActionResult DeleteDiscount(
        [FromRoute] int businessId,
        [FromRoute] int discountId)
    {
        // delete and return NoContent();
        return NoContent();
    }

    // ===== ELIGIBILITIES (scoped to a discount) =====

    /// <summary>List eligibilities (catalog items) for a discount.</summary>
    [HttpGet("{discountId:int}/eligibilities")]
    public ActionResult<IEnumerable<DiscountEligibilityResponse>> ListEligibilities(
        [FromRoute] int businessId,
        [FromRoute] int discountId)
    {
        // return Ok(mappedResults);
        return Ok(Array.Empty<DiscountEligibilityResponse>());
    }

    /// <summary>Add an eligibility row for this discount.</summary>
    [HttpPost("{discountId:int}/eligibilities")]
    public IActionResult AddEligibility(
        [FromRoute] int businessId,
        [FromRoute] int discountId,
        [FromBody] CreateDiscountEligibilityRequest body)
    {
        // create and return CreatedAtAction (or 201)
        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>Remove an eligibility for specific CatalogItem.</summary>
    [HttpDelete("{discountId:int}/eligibilities/{catalogItemId:int}")]
    public IActionResult RemoveEligibility(
        [FromRoute] int businessId,
        [FromRoute] int discountId,
        [FromRoute] int catalogItemId)
    {
        // delete and return NoContent();
        return NoContent();
    }
}