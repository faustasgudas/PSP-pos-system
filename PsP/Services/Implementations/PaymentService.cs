using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Payments;
using PsP.Data;
using PsP.Models;
using PsP.Services.Interfaces;

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

    public async Task<PaymentResponse> CreatePaymentAsync(
        long amountCents,
        string currency,
        int businessId,
        int orderId,
        string? giftCardCode,
        long? giftCardAmountCents,
        string baseUrl)
    {
        if (amountCents <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountCents));

        // ----- tikrinam, kad order egzistuoja ir priklauso tam business -----
        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order is null)
            throw new InvalidOperationException("order_not_found");

        if (order.BusinessId != businessId)
            throw new InvalidOperationException("wrong_business");

        GiftCard? card = null;
        long plannedFromGiftCard = 0;
        long remainingForStripe = amountCents;

        // ---------- GIFT CARD DALIS ----------
        if (!string.IsNullOrWhiteSpace(giftCardCode))
        {
            card = await _giftCards.GetByCodeAsync(giftCardCode)
                   ?? throw new InvalidOperationException("invalid_gift_card");

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
                // jei nenurodytas konkretus kiekis – imam kiek galima iki sumos
                plannedFromGiftCard = maxFromCard;
            }

            remainingForStripe = amountCents - plannedFromGiftCard;
        }

        // nusprendžiam metodą
        string method;
        if (plannedFromGiftCard == 0 && remainingForStripe > 0)
            method = "Stripe";
        else if (plannedFromGiftCard > 0 && remainingForStripe == 0)
            method = "GiftCard";
        else
            method = "GiftCard+Stripe";

        // ---------- PAYMENT ĮRAŠAS DB ----------
        var p = new Payment
        {
            AmountCents          = amountCents,
            Currency             = currency,
            CreatedAt            = DateTime.UtcNow,
            Status               = "Pending",
            Method               = method,
            GiftCardId           = plannedFromGiftCard > 0 ? card?.GiftCardId : null,
            BusinessId           = businessId,
            OrderId              = orderId,
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

            stripeUrl       = session.Url;
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

            p.Status      = "Success";
            p.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return new PaymentResponse(
            p.PaymentId,
            plannedFromGiftCard,
            remainingForStripe,
            stripeUrl,
            stripeSessionId
        );
    }

    // kviečiama iš /api/payments/success
    public async Task ConfirmStripeSuccessAsync(string sessionId)
    {
        var p = await _db.Payments
            .Include(x => x.GiftCard)
            .FirstOrDefaultAsync(x => x.StripeSessionId == sessionId);

        if (p == null) return;
        if (p.Status == "Success") return; // idempotency

        // jei yra giftcard dalis – nurašom dabar
        if (p.GiftCardId is not null &&
            p.GiftCardPlannedCents > 0)
        {
            try
            {
                var (charged, _) = await _giftCards.RedeemAsync(
                    p.GiftCardId.Value,
                    p.GiftCardPlannedCents
                );

                // jei dėl kokios nors priežasties buvo mažiau nei planuota – fiksuojam realiai nurašytą
                p.GiftCardPlannedCents = charged;
            }
            catch
            {
                // čia galima loginti / markinti specialų statusą,
                // bet Stripe dalis vis tiek pavyko, todėl payment laikom kaip Success
                // ir neblokuojam srauto.
            }
        }

        p.Status      = "Success";
        p.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}