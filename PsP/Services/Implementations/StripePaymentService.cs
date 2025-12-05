using Microsoft.Extensions.Options;
using PsP.Services.Interfaces;
using PsP.Settings;
using Stripe;
using Stripe.Checkout;

namespace PsP.Services.Implementations;

public class StripePaymentService : IStripePaymentService
{
    private readonly StripeSettings _settings;

    public StripePaymentService(IOptions<StripeSettings> stripeSettings)
    {
        _settings = stripeSettings.Value;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public Session CreateCheckoutSession(
        long amountCents,
        string currency,
        string successUrl,
        string cancelUrl,
        int paymentId)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl  = cancelUrl,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = amountCents,
                        Currency   = currency.ToLower(),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Cart payment"
                        },
                    }
                },
            },
            Metadata = new Dictionary<string, string>
            {
                { "paymentId", paymentId.ToString() }
            }
        };

        var service = new SessionService();
        return service.Create(options);
    }

    public async Task RefundAsync(
        string stripeSessionId,
        long amountCents,
        CancellationToken ct = default)
    {
        var sessionService = new SessionService();
        var session = await sessionService.GetAsync(stripeSessionId, cancellationToken: ct);

        if (session.PaymentIntent == null)
            throw new InvalidOperationException("stripe_payment_intent_missing");

        var refundService = new RefundService();
        var options = new RefundCreateOptions
        {
            PaymentIntent = session.PaymentIntent.Id, // 👈 čia ID
            Amount        = amountCents
        };

        await refundService.CreateAsync(options, cancellationToken: ct);
    }

}
