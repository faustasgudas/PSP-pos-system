using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PsP.Contracts.Common;
using PsP.Contracts.Payments;
using PsP.Services.Implementations;

namespace PsP.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _payments;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(PaymentService payments, ILogger<PaymentController> logger)
    {
        _payments = payments;
        _logger = logger;
    }

    /// <summary>
    /// Sukuria naują apmokėjimą (Stripe + optional GiftCard).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentResult>> Create([FromBody] CreatePaymentRequest request)
    {
        _logger.LogInformation(
            "Creating payment for business {BusinessId}, amount {AmountCents} {Currency}, giftCard: {GiftCardCode}",
            request.BusinessId, request.AmountCents, request.Currency, request.GiftCardCode
        );

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var result = await _payments.CreatePaymentAsync(
                request.AmountCents,
                request.Currency,
                request.BusinessId,
                request.GiftCardCode,
                request.GiftCardAmountCents,
                baseUrl);

            return Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid amount for payment");
            return BadRequest(new ApiErrorResponse("Invalid payment amount", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            // pvz. invalid_gift_card / wrong_business / blocked / expired
            _logger.LogWarning(ex, "Business rule violation when creating payment");
            return BadRequest(new ApiErrorResponse("Payment failed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when creating payment");
            return BadRequest(new ApiErrorResponse("Unexpected error while creating payment", ex.Message));
        }
    }

    /// <summary>
    /// Stripe success callback (/api/payments/success?sessionId=...).
    /// </summary>
    [HttpGet("success")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Success([FromQuery] string sessionId)
    {
        _logger.LogInformation("Stripe payment success callback. SessionId: {SessionId}", sessionId);

        await _payments.ConfirmStripeSuccessAsync(sessionId);

        // jei norėsi – gali gražinti daugiau info apie paymentą
        return Ok("Payment successful.");
    }

    /// <summary>
    /// Stripe cancel callback (/api/payments/cancel?sessionId=...).
    /// </summary>
    [HttpGet("cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Cancel([FromQuery] string sessionId)
    {
        _logger.LogInformation("Stripe payment cancelled. SessionId: {SessionId}", sessionId);

        // čia galėtum atnaujinti Payment.Status į "Cancelled", jei norėsi
        return Ok("Payment cancelled.");
    }
}
