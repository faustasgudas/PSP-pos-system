using Microsoft.EntityFrameworkCore;
using PsP.Data;
using PsP.Models;
using PsP.Services.Interfaces; // čia IGiftCardService

namespace PsP.Services.Implementations;

public class PaymentService
{
    private readonly AppDbContext _db;
    private readonly IGiftCardService _giftCards;
    private readonly StripePaymentService _stripe;

    public PaymentService(AppDbContext db, IGiftCardService giftCards, StripePaymentService stripe)
    {
        _db = db;
        _giftCards = giftCards;
        _stripe = stripe;
    }

    public async Task<PaymentResult> CreatePaymentAsync(
        long amountCents,
        string currency,
        int businessId,
        string? giftCardCode,
        long? giftCardAmountCents,
        string baseUrl)
    {
        GiftCard? card = null;
        long plannedFromGiftCard = 0;
        long remainingForStripe = amountCents;

        // ---------- GIFT CARD DALIS ----------
        if (!string.IsNullOrWhiteSpace(giftCardCode))
        {
            card = await _giftCards.GetByCodeAsync(giftCardCode)
                   ?? throw new InvalidOperationException("invalid_gift_card");

            // verslo taisyklės (seniau buvo ValidateAsync)
            if (card.BusinessId != businessId)
                throw new InvalidOperationException("wrong_business");

            if (card.Status != "Active")
                throw new InvalidOperationException("blocked");

            if (card.ExpiresAt is not null && card.ExpiresAt <= DateTime.UtcNow)
                throw new InvalidOperationException("expired");

            var maxFromCard = Math.Min(card.Balance, amountCents);

            if (giftCardAmountCents.HasValue)
            {
                if (giftCardAmountCents.Value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(giftCardAmountCents));

                plannedFromGiftCard = Math.Min(giftCardAmountCents.Value, maxFromCard);
            }
            else
            {
                // senas elgesys: naudoti maksimumą iš kortelės
                plannedFromGiftCard = maxFromCard;
            }

            remainingForStripe = amountCents - plannedFromGiftCard;
        }

        // ---------- PAYMENT ĮRAŠAS DB ----------
        var p = new Payment
        {
            AmountCents          = amountCents,
            Currency             = currency,
            CreatedAt            = DateTime.UtcNow,
            Status               = "Pending",
            Method               = remainingForStripe == 0 ? "GiftCard" : "GiftCard+Stripe",
            GiftCardId           = card?.GiftCardId,
            BusinessId           = businessId,
            GiftCardPlannedCents = plannedFromGiftCard
        };

        _db.Payments.Add(p);
        await _db.SaveChangesAsync();

        string? stripeUrl = null;
        string? stripeSessionId = null;

        // ---------- STRIPE DALIS ----------
        if (remainingForStripe > 0)
        {
            var successUrl = $"{baseUrl}/api/payments/success?sessionId={{CHECKOUT_SESSION_ID}}";
            var cancelUrl  = $"{baseUrl}/api/payments/cancel?sessionId={{CHECKOUT_SESSION_ID}}";

            var session = _stripe.CreateCheckoutSession(
                remainingForStripe,
                currency,
                successUrl,
                cancelUrl,
                p.PaymentId
            );

            stripeUrl = session.Url;
            stripeSessionId = session.Id;

            p.StripeSessionId = session.Id;
            await _db.SaveChangesAsync();
        }
        else
        {
            // 100% apmokėjimas gift card'u – nurašom iškart
            if (card is not null && plannedFromGiftCard > 0)
            {
                await _giftCards.RedeemAsync(card.GiftCardId, plannedFromGiftCard);
            }

            p.Status = "Success";
            p.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return new PaymentResult(
            p.PaymentId,
            plannedFromGiftCard,
            remainingForStripe,
            stripeUrl,
            stripeSessionId
        );
    }

    // kviečiama iš /api/payment/success
    public async Task ConfirmStripeSuccessAsync(string sessionId)
    {
        var p = await _db.Payments
            .Include(x => x.GiftCard)
            .FirstOrDefaultAsync(x => x.StripeSessionId == sessionId);

        if (p == null) return;
        if (p.Status == "Success") return; // idempotency

        if (p.GiftCardId is not null &&
            p.GiftCardPlannedCents > 0 &&
            p.GiftCard is not null)
        {
            // čia irgi pereinam prie Redeem pagal ID
            await _giftCards.RedeemAsync(
                p.GiftCard.GiftCardId,
                p.GiftCardPlannedCents
            );
        }

        p.Status = "Success";
        p.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}

public class PaymentResult
{
    public int PaymentId { get; set; }
    public long PaidByGiftCard { get; set; }
    public long RemainingForStripe { get; set; }
    public string? StripeUrl { get; set; }
    public string? StripeSessionId { get; set; }

    public PaymentResult(
        int paymentId,
        long paidByGiftCard,
        long remainingForStripe,
        string? stripeUrl,
        string? stripeSessionId)
    {
        PaymentId = paymentId;
        PaidByGiftCard = paidByGiftCard;
        RemainingForStripe = remainingForStripe;
        StripeUrl = stripeUrl;
        StripeSessionId = stripeSessionId;
    }
}
