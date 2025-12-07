using Microsoft.AspNetCore.Mvc;
using PsP.Contracts.StockItems;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/stock-items")]
public class StockItemsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<StockItemSummaryResponse>> ListAll(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId,
        [FromQuery] int? catalogItemId = null)
    {
        return Ok();
    }

    [HttpGet("{stockItemId:int}")]
    public ActionResult<StockItemDetailResponse> GetOne(
        [FromRoute] int businessId,
        [FromRoute] int stockItemId,
        [FromQuery] int callerEmployeeId)
    {
        return Ok();
    }

    [HttpPost]
    public IActionResult Create(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId,
        [FromBody] CreateStockItemRequest body)
    {
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{stockItemId:int}")]
    public IActionResult Update(
        [FromRoute] int businessId,
        [FromRoute] int stockItemId,
        [FromQuery] int callerEmployeeId,
        [FromBody] UpdateStockItemRequest body)
    {
        return Ok();
    }
}