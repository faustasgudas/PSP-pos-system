using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PsP.Contracts.Common;
using PsP.Contracts.Payments;
using PsP.Services.Interfaces;

namespace PsP.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService payments, ILogger<PaymentController> logger)
    {
        _payments = payments;
        _logger = logger;
    }

    private int GetBusinessIdFromToken()
    {
        var claim = User.FindFirst("businessId")
                    ?? throw new InvalidOperationException("Missing businessId claim");
        return int.Parse(claim.Value);
    }

    /// <summary>
    /// Sukuria naują apmokėjimą (Stripe + optional GiftCard).
    /// Suma visada skaičiuojama backend'e pagal Order eilutes.
    /// </summary>
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

            var result = await _payments.CreatePaymentAsync(
                orderId: request.OrderId,
                businessId: businessId,
                callerEmployeeId: callerEmployeeId,
                giftCardCode: request.GiftCardCode,
                giftCardAmountCents: request.GiftCardAmountCents,
                tipCents: request.TipCents,
                baseUrl: baseUrl
            );

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse("Payment failed", ex.Message));
        }
    }


    [AllowAnonymous]
    [HttpGet("success")]
    public IActionResult Success([FromQuery] string sessionId)
        => Ok(new { message = "payment_processing", sessionId });

    [AllowAnonymous]
    [HttpGet("cancel")]
    public async Task<IActionResult> Cancel([FromQuery] string sessionId)
    {
        await _payments.CancelStripeAsync(sessionId);
        return Ok(new { message = "payment_cancelled", sessionId });
    }


    /// <summary>
    /// Full refund (MVP). Galėtų būti tik Owner/Manager – jei nori, pridėk [Authorize(Roles="Owner,Manager")].
    /// </summary>
    [HttpPost("{paymentId:int}/refund")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refund(int paymentId)
    {
        try
        {
            await _payments.RefundFullAsync(paymentId);
            return Ok(new { message = "Refund processed" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Refund failed for {PaymentId}", paymentId);
            return BadRequest(new ApiErrorResponse("Refund failed", ex.Message));
        }
    }

    /// <summary>
    /// Visi business payment'ai (pagal JWT businessId).
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetPaymentsForBusiness()
    {
        var businessId = GetBusinessIdFromToken();
        var list = await _payments.GetPaymentsForBusinessAsync(businessId);
        return Ok(list); // jei norėsi – perdaryk į DTO
    }

    /// <summary>
    /// Vieno order payment'ai (pagal JWT businessId).
    /// </summary>
    [HttpGet("orders/{orderId:int}")]
    public async Task<IActionResult> GetPaymentsForOrder(int orderId)
    {
        var businessId = GetBusinessIdFromToken();
        var list = await _payments.GetPaymentsForOrderAsync(businessId, orderId);
        return Ok(list);
    }
    
    private int GetEmployeeIdFromToken()
    {
        var claim = User.FindFirst("employeeId")
                    ?? throw new InvalidOperationException("Missing employeeId claim");
        return int.Parse(claim.Value);
    }

}
    