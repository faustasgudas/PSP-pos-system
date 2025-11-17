using Microsoft.AspNetCore.Mvc;
using PsP.Models;

namespace PsP.Controllers;

[ApiController]
[Route("api/businesses/{businessId:int}/orders")]
public class OrdersController : ControllerBase
{
    // Managers/Owners: list ALL. Staff: should be rejected in service.
    [HttpGet("ListAllOrders")]
    public ActionResult<IEnumerable<OrderSummaryDto>> ListAll(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId,
        [FromQuery] string? status = null, // Open | Closed | Cancelled | Refunded
        [FromQuery] DateTime? from = null, // createdAt >= from
        [FromQuery] DateTime? to = null) // createdAt <= to
    {
        return Ok();
    }

    // Caller’s own orders (for Staff typically only open ones; enforce in service)
    [HttpGet("ListMyOrders")]
    public ActionResult<IEnumerable<OrderSummaryDto>> ListMine(
        [FromRoute] int businessId,
        [FromQuery] int callerEmployeeId
    ) // default: workers see only open
    {
        return Ok();
    }

    // Get one order (Staff only if they own it; Managers/Owners allowed)
    [HttpGet("GetOrder/{orderId:int}")]
    public ActionResult<OrderDetailDto> GetOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId)
    {
        return Ok();
    }

    // LIST all lines for an order
// GET /api/businesses/{businessId}/orders/{orderId}/lines
    [HttpGet("GetOrder/{orderId:int}/ListOrderLines")]
    public ActionResult<IEnumerable<OrderLineDto>> ListLines(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId)
    {
        return Ok();
    }

    // GET a single line
// GET /api/businesses/{businessId}/orders/{orderId}/lines/{orderLineId}

    [HttpGet("GetOrder/{orderId:int}/OrderLine/{orderLineId:int}")]
    public ActionResult<OrderLineDto> GetLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromRoute] int orderLineId,
        [FromQuery] int callerEmployeeId)
    {
        return NotFound();
    }




    [HttpPost]
    public IActionResult CreateOrder(
        [FromRoute] int businessId,
        [FromBody] CreateOrderBody body)
    {
        return StatusCode(StatusCodes.Status201Created);
    }

    // Update an OPEN order (move table, set/cancel status, set order-level discountId, tip)
    [HttpPut("{orderId:int}")]
    public IActionResult UpdateOpenOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId,
        [FromBody] UpdateOrderBody body)
    {
        return Ok();
    }


    [HttpPost("{orderId:int}/close")]
    public IActionResult CloseOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId)
    {
        return Ok();
    }

    // Cancel an OPEN order (action)
    [HttpPost("{orderId:int}/cancel")]
    public IActionResult CancelOrder(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId,
        [FromBody] CancelOrderBody body)
    {
        return Ok();
    }


    [HttpPost("{orderId:int}/lines")]
    public IActionResult AddLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromQuery] int callerEmployeeId,
        [FromBody] AddLineBody body)
    {
        return StatusCode(StatusCodes.Status201Created);
    }

    // Update a line on an OPEN order
    [HttpPut("{orderId:int}/lines/{orderLineId:int}")]
    public IActionResult UpdateLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromRoute] int orderLineId,
        [FromQuery] int callerEmployeeId,
        [FromBody] UpdateLineBody body)
    {
        return Ok();
    }

    [HttpDelete("{orderId:int}/lines/{orderLineId:int}")]
    public IActionResult RemoveLine(
        [FromRoute] int businessId,
        [FromRoute] int orderId,
        [FromRoute] int orderLineId,
        [FromQuery] int callerEmployeeId)
    {
        return NoContent();
    }
}

public class CreateOrderBody
    {
        public int employeeId { get; set; }             // who opens it
        public int? reservationId { get; set; }         // optional link
        public string? tableOrArea { get; set; }
    }

    public class UpdateOrderBody
    {
        public int employeeId { get; set; }             
        public string? status { get; set; }             // "Open" | "Cancelled" (keep Open-only here)
        public string? tableOrArea { get; set; }
        public string? tipAmount { get; set; }          // "12.50"
        public int? discountId { get; set; }            // order-level discount (optional)
    }

   

    public class CancelOrderBody
    {
        public int employeeId { get; set; }             // who cancels
        public string? reason { get; set; }             // optional note
    }

   

    public class AddLineBody
    {
        public int catalogItemId { get; set; }          // points to CatalogItem
        public decimal qty { get; set; }
        public int? discountId { get; set; }            // line-level discount intent (optional)
    }

    public class UpdateLineBody
    {
        public decimal? qty { get; set; }
        public int? discountId { get; set; }            // overwrite/clear discount
    }
    
    public record OrderSummaryDto(
        int OrderId,
        int BusinessId,
        int EmployeeId,
        int? ReservationId,
        string Status,
        string? TableOrArea,
        DateTime CreatedAt,
        DateTime? ClosedAt,
        decimal TipAmount,
        int? DiscountId
    );

    public record OrderDetailDto(
        int OrderId ,
        int BusinessId,
        int EmployeeId,
        int? ReservationId,
        string Status,
        string? TableOrArea,
        DateTime CreatedAt,
        DateTime? ClosedAt,
        decimal TipAmount,
        int? DiscountId,
        string? OrderDiscountSnapshot,
        List<OrderLineDto> Lines
    );

    public record OrderLineDto(
        int OrderLineId,
        int OrderId,
        int BusinessId,
        int CatalogItemId,
        int? DiscountId,
        decimal Qty,
        string ItemNameSnapshot,
        decimal UnitPriceSnapshot,
        string? UnitDiscountSnapshot,
        string TaxClassSnapshot,
        decimal TaxRateSnapshotPct,
        DateTime PerformedAt,
        int? PerformedByEmployeeId
    );

    
    
    
    
