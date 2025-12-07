using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsP.Auth;
using PsP.Contracts.Catalog;
using PsP.Services.Interfaces;

namespace PsP.Api.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/catalog-items")]
[Authorize]
public class CatalogItemsController : ControllerBase
{
    private readonly ICatalogItemsService _catalogItems;

    public CatalogItemsController(ICatalogItemsService catalogItems)
    {
        _catalogItems = catalogItems;
    }

    private ActionResult? EnsureBusinessMatchesRoute(int routeBusinessId)
    {
        var jwtBizId = User.GetBusinessId();
        if (jwtBizId != routeBusinessId)
            return Forbid();

        return null;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CatalogItemSummaryResponse>>> GetAll(
        int businessId,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] string? code = null)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var employeeId = User.GetEmployeeId();

        var result = await _catalogItems.ListAllAsync(
            businessId,
            callerEmployeeId: employeeId,
            type,
            status,
            code);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CatalogItemDetailResponse>> GetById(
        int businessId,
        int id)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var employeeId = User.GetEmployeeId();

        var result = await _catalogItems.GetByIdAsync(
            businessId,
            id,
            callerEmployeeId: employeeId);

        if (result is null) return NotFound();
        return Ok(result);
    }
    [HttpPost]
    public async Task<ActionResult<CatalogItemDetailResponse>> Create(
        int businessId,
        [FromBody] CreateCatalogItemRequest body)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var employeeId = User.GetEmployeeId();

        try
        {
            var created = await _catalogItems.CreateAsync(
                businessId,
                callerEmployeeId: employeeId,
                body);

            return CreatedAtAction(
                nameof(GetById),
                new { businessId, id = created.CatalogItemId },
                created);
        }
        catch (InvalidOperationException ex) when (ex.Message == "catalog_item_code_already_exists")
        {
            return Conflict(new { error = "catalog_item_code_already_exists" });
        }
    }


    [HttpPut("{id:int}")]
    public async Task<ActionResult<CatalogItemDetailResponse>> Update(
        int businessId,
        int id,
        [FromBody] UpdateCatalogItemRequest body)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var employeeId = User.GetEmployeeId();

        var updated = await _catalogItems.UpdateAsync(
            businessId,
            id,
            callerEmployeeId: employeeId,
            body);

        if (updated is null) return NotFound();
        return Ok(updated);
    }

    [HttpPost("{id:int}/archive")]
    public async Task<IActionResult> Archive(int businessId, int id)
    {
        var mismatch = EnsureBusinessMatchesRoute(businessId);
        if (mismatch is not null) return mismatch;

        var employeeId = User.GetEmployeeId();

        var ok = await _catalogItems.ArchiveAsync(
            businessId,
            id,
            callerEmployeeId: employeeId);

        if (!ok) return NotFound();
        return NoContent();
    }
}
