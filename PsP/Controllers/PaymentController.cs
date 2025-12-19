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


    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentResponse>> Create([FromBody] CreatePaymentRequest request)
    {
        var businessId = GetBusinessIdFromToken();

        _logger.LogInformation(
            "Creating payment for business {BusinessId}, order {OrderId}, currency {Currency}, giftCard: {GiftCardCode}",
            businessId, request.OrderId, request.Currency, request.GiftCardCode
        );

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

           
            var result = await _payments.CreatePaymentAsync(
                orderId: request.OrderId,
                currency: request.Currency,
                businessId: businessId,
                giftCardCode: request.GiftCardCode,
                giftCardAmountCents: request.GiftCardAmountCents,
                baseUrl: baseUrl
            );

            return Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
           
            _logger.LogWarning(ex, "Invalid argument when creating payment");
            return BadRequest(new ApiErrorResponse("Invalid payment data", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
           
            _logger.LogWarning(ex, "Business rule violation when creating payment");
            return BadRequest(new ApiErrorResponse("Payment failed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when creating payment");
            return BadRequest(new ApiErrorResponse("Unexpected error while creating payment", ex.Message));
        }
    }


    [AllowAnonymous]
    [HttpGet("success")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Success([FromQuery] string sessionId)
    {
        _logger.LogInformation("Stripe payment success callback. SessionId: {SessionId}", sessionId);

        await _payments.ConfirmStripeSuccessAsync(sessionId);

     
        return Ok("Payment successful.");
    }


    [AllowAnonymous]
    [HttpGet("cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Cancel([FromQuery] string sessionId)
    {
        _logger.LogInformation("Stripe payment cancelled. SessionId: {SessionId}", sessionId);

     
        return Ok("Payment cancelled.");
    }


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


    [HttpGet("history")]
    public async Task<IActionResult> GetPaymentsForBusiness()
    {
        var businessId = GetBusinessIdFromToken();
        var list = await _payments.GetPaymentsForBusinessAsync(businessId);
        return Ok(list);
    }

 
    [HttpGet("orders/{orderId:int}")]
    public async Task<IActionResult> GetPaymentsForOrder(int orderId)
    {
        var businessId = GetBusinessIdFromToken();
        var list = await _payments.GetPaymentsForOrderAsync(businessId, orderId);
        return Ok(list);
    }
}
    