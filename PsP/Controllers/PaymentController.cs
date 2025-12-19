using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Common;
using PsP.Contracts.Orders;
using PsP.Data;
using PsP.Services.Interfaces;
using PsP.Contracts.Payments;
using PsP.Contracts.Orders;

namespace PsP.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly ILogger<PaymentController> _logger;
    private readonly AppDbContext _db;
    private readonly IOrdersService _orders;

    public PaymentController(IPaymentService payments, IOrdersService orders, AppDbContext db, ILogger<PaymentController> logger)
    {
        _payments = payments;
        _orders = orders;
        _db = db;
        _logger = logger;
    }

    private int GetBusinessIdFromToken()
    {
        var claim = User.FindFirst("businessId")
                    ?? throw new InvalidOperationException("Missing businessId claim");
        return int.Parse(claim.Value);
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentResponse>> Create([FromBody] CreatePaymentRequest request)
    {
        var businessId = GetBusinessIdFromToken();
        var callerEmployeeId = GetEmployeeIdFromToken();

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var frontendBaseUrl = "http://localhost:5173"; 

            var result = await _payments.CreatePaymentAsync(
                orderId: request.OrderId,
                businessId: businessId,
                callerEmployeeId: callerEmployeeId,
                giftCardCode: request.GiftCardCode,
                giftCardAmountCents: request.GiftCardAmountCents,
                tipCents: request.TipCents,
                baseUrl: frontendBaseUrl
            );


            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse("Payment failed", ex.Message));
        }
    }
    
    [HttpPost("{paymentId:int}/refund")]
    [Authorize(Roles = "Owner,Manager")]
    [ProducesResponseType(typeof(OrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refund(
        int paymentId,
        [FromBody] CancelOrderRequest request,
        CancellationToken ct)
    {
        var businessId = GetBusinessIdFromToken();
        var callerEmployeeId = GetEmployeeIdFromToken();

        try
        {
            var p = await _db.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PaymentId == paymentId, ct)
                ?? throw new InvalidOperationException("payment_not_found");

            if (p.BusinessId != businessId)
                throw new InvalidOperationException("wrong_business");

            var order = await _db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.BusinessId == businessId && o.OrderId == p.OrderId, ct)
                ?? throw new InvalidOperationException("order_not_found");

            if (!string.Equals(order.Status, "Closed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("order_not_closed");

            await _payments.RefundFullAsync(paymentId, ct);

            var result = await _orders.RefundOrderAsync(
                businessId,
                p.OrderId,
                callerEmployeeId,
                request,
                ct);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Refund failed for PaymentId={PaymentId}", paymentId);
            return BadRequest(new ApiErrorResponse("Refund failed", ex.Message));
        }
    }



    [HttpGet("history")]
    public async Task<IActionResult> GetPaymentsForBusiness()
    {
        var businessId = GetBusinessIdFromToken();
        var list = await _payments.GetPaymentsForBusinessAsync(businessId);
        return Ok(list); // jei norėsi – perdaryk į DTO
    }


    [HttpGet("orders/{orderId:int}")]
    public async Task<IActionResult> GetPaymentsForOrder(int orderId)
    {
        var businessId = GetBusinessIdFromToken();
        var list = await _payments.GetPaymentsForOrderAsync(businessId, orderId);
        return Ok(list);
    }
    [AllowAnonymous]
    [HttpPost("stripe/cancel")]
    public async Task<IActionResult> CancelStripe([FromQuery] string sessionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(new ApiErrorResponse("Cancel failed", "missing_sessionId"));

        await _payments.CancelStripeAsync(sessionId, ct);
        return Ok();
    }


    
    private int GetEmployeeIdFromToken()
    {
        var claim = User.FindFirst("employeeId")
                    ?? throw new InvalidOperationException("Missing employeeId claim");
        return int.Parse(claim.Value);
    }

}
    