using Microsoft.AspNetCore.Mvc;
using PsP.Contracts.StockMovements;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/stock-items/{stockItemId:int}/movements")]
public class StockMovementsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<StockMovementResponse>> ListForStockItem(
        [FromRoute] int businessId,
        [FromRoute] int stockItemId,
        [FromQuery] int callerEmployeeId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? type = null) // "Receive" | "Sale" | "RefundReturn" | "Waste" | "Adjust"
    {
        return Ok();
    }

    [HttpGet("{movementId:int}")]
    public ActionResult<StockMovementResponse> GetOne(
        [FromRoute] int businessId,
        [FromRoute] int stockItemId,
        [FromRoute] int movementId,
        [FromQuery] int callerEmployeeId)
    {
        return Ok();
    }

    [HttpPost]
    public IActionResult Create(
        [FromRoute] int businessId,
        [FromRoute] int stockItemId,
        [FromQuery] int callerEmployeeId,
        [FromBody] CreateStockMovementRequest body)
    {
        return StatusCode(StatusCodes.Status201Created);
    }
}