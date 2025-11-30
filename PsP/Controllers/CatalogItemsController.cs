using Microsoft.AspNetCore.Mvc;
using PsP.Contracts.Catalog;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/catalog-items")]
public class CatalogItemsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<CatalogItemSummaryResponse>> ListAll(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId,
        [FromQuery] string? type = null,     // "Product" | "Service"
        [FromQuery] string? status = null,   // "Draft" | "Active" | "Archived"
        [FromQuery] string? code = null)
    {
        return Ok();
    }

    [HttpGet("{catalogItemId:int}")]
    public ActionResult<CatalogItemDetailResponse> GetOne(
        [FromRoute] int businessId,
        [FromRoute] int catalogItemId,
        [FromQuery] int callerEmployeeId)
    {
        return Ok();
    }

    [HttpPost]
    public IActionResult Create(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId,
        [FromBody] CreateCatalogItemRequest body)
    {
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{catalogItemId:int}")]
    public IActionResult Update(
        [FromRoute] int businessId,
        [FromRoute] int catalogItemId,
        [FromQuery] int callerEmployeeId,
        [FromBody] UpdateCatalogItemRequest body)
    {
        return Ok();
    }

    // Archive (action) without PATCH
    [HttpPost("{catalogItemId:int}/archive")]
    public IActionResult Archive(
        [FromRoute] int businessId,
        [FromRoute] int catalogItemId,
        [FromQuery] int callerEmployeeId)
    {
        return Ok();
    }
}
