using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PsP.Services.Interfaces;
using PsP.Settings;
using Stripe;
using Stripe.Checkout;

namespace PsP.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly StripeSettings _stripe;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IPaymentService payments,
        IOptions<StripeSettings> stripe,
        ILogger<WebhooksController> logger)
    {
        _payments = payments;
        _stripe = stripe.Value;
        _logger = logger;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();
        var sigHeader = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrWhiteSpace(_stripe.WebhookSecret))
            return BadRequest("webhook_secret_missing");

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                sigHeader,
                _stripe.WebhookSecret
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed");
            return BadRequest("invalid_signature");
        }

        _logger.LogInformation("Stripe webhook type={Type}", stripeEvent.Type);
        _logger.LogInformation("Stripe webhook objectType={ObjType}", stripeEvent.Data?.Object?.GetType().FullName);

        try
        {
            if (stripeEvent.Type == "checkout.session.completed")
            {
                // ✅ patikimiausias būdas Stripe.NET'e
                var session = stripeEvent.Data.Object as Session
                              ?? stripeEvent.Data.Object as Stripe.Checkout.Session;

                // jei tavo Stripe.NET palaiko:
                // var session = stripeEvent.Data.Object as Session ?? stripeEvent.Data.Object as Stripe.Checkout.Session;
                // arba:
                // var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                // (aukščiau palikta su dviem tipais dėl versijų)

                var sessionId = session?.Id;

                _logger.LogInformation("checkout.session.completed sessionId={SessionId}", sessionId);

                if (!string.IsNullOrWhiteSpace(sessionId))
                    await _payments.ConfirmStripeSuccessAsync(sessionId);
            }

            if (stripeEvent.Type == "checkout.session.expired")
            {
                var session = stripeEvent.Data.Object as Session
                              ?? stripeEvent.Data.Object as Stripe.Checkout.Session;

                var sessionId = session?.Id;

                _logger.LogInformation("checkout.session.expired sessionId={SessionId}", sessionId);

                if (!string.IsNullOrWhiteSpace(sessionId))
                    await _payments.CancelStripeAsync(sessionId);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook processing error");
            return StatusCode(500);
        }
    }
}
