using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Payments;
using PsP.Data;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _db;
        private readonly IGiftCardService _giftCards;
        private readonly IStripePaymentService _stripe;

        public PaymentService(
            AppDbContext db,
            IGiftCardService giftCards,
            IStripePaymentService stripe)
        {
            _db = db;
            _giftCards = giftCards;
            _stripe = stripe;
        }

       public async Task<PaymentResponse> CreatePaymentAsync(
    int orderId,
    string currency,
    int businessId,
    string? giftCardCode,
    long? giftCardAmountCents,
    string baseUrl)
{
    // 1) Order + lines
    var order = await _db.Orders
        .Include(o => o.Lines)
        .ThenInclude(ol => ol.CatalogItem)
        .AsNoTracking()
        .FirstOrDefaultAsync(o => o.OrderId == (int)orderId); // 👈 cast į int, jei OrderId yra int

    if (order is null)
        throw new InvalidOperationException("order_not_found");

    if (order.BusinessId != businessId)
        throw new InvalidOperationException("wrong_business");

    // 2) server-side total
    var amountCents = CalculateOrderTotal(order);
    if (amountCents <= 0)
        throw new InvalidOperationException("invalid_order_total");

    GiftCard? card = null;
    long plannedFromGiftCard = 0;
    long remainingForStripe  = amountCents;

    // 3) gift card logic
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
            plannedFromGiftCard = maxFromCard;
        }

        remainingForStripe = amountCents - plannedFromGiftCard;
    }

    string method;
    if (plannedFromGiftCard == 0 && remainingForStripe > 0)
        method = "Stripe";
    else if (plannedFromGiftCard > 0 && remainingForStripe == 0)
        method = "GiftCard";
    else
        method = "GiftCard+Stripe";

    var p = new Payment
    {
        AmountCents          = amountCents,
        Currency             = currency,
        CreatedAt            = DateTime.UtcNow,
        Status               = "Pending",
        Method               = method,
        GiftCardId           = plannedFromGiftCard > 0 ? card?.GiftCardId : null,
        BusinessId           = businessId,
        OrderId              = (int)orderId, // 👈 ir čia cast
        GiftCardPlannedCents = plannedFromGiftCard
    };

    _db.Payments.Add(p);
    await _db.SaveChangesAsync();

    string? stripeUrl = null;
    string? stripeSessionId = null;

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
        await using var tx = await _db.Database.BeginTransactionAsync();

        if (card is not null && plannedFromGiftCard > 0)
        {
            await _giftCards.RedeemAsync(card.GiftCardId, plannedFromGiftCard, businessId);
        }

        p.Status      = "Success";
        p.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    return new PaymentResponse(
        p.PaymentId,
        plannedFromGiftCard,
        remainingForStripe,
        stripeUrl,
        stripeSessionId
    );
}


private static long CalculateOrderTotal(Order order)
{
    long totalCents = 0;

    foreach (var line in order.Lines)
    {
        if (line.UnitPriceSnapshot <= 0)
            throw new InvalidOperationException("missing_price_snapshot");

        // 1) suma eurais už eilutę
        decimal lineTotalEur = line.UnitPriceSnapshot * line.Qty;

        // 2) paverčiam į centus
        long lineTotalCents = (long)Math.Round(
            lineTotalEur * 100m,
            MidpointRounding.AwayFromZero
        );

        totalCents += lineTotalCents;
    }

    return totalCents;
}

        public async Task ConfirmStripeSuccessAsync(string sessionId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            var p = await _db.Payments
                .Include(x => x.GiftCard)
                .FirstOrDefaultAsync(x => x.StripeSessionId == sessionId);

            if (p == null)
                return;

            if (p.Status == "Success")
                return;

            if (p.GiftCardId is not null && p.GiftCardPlannedCents > 0)
            {
                try
                {
                    var (charged, _) = await _giftCards.RedeemAsync(
                        p.GiftCardId.Value,
                        p.GiftCardPlannedCents,
                        p.BusinessId
                    );

                    // saugom realiai nuskaitytą sumą
                    p.GiftCardPlannedCents = charged;
                }
                catch
                {
                }
            }

            p.Status      = "Success";
            p.CompletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public async Task RefundFullAsync(int paymentId)
        {
            // Pasiimam payment
            var p = await _db.Payments
                .FirstOrDefaultAsync(x => x.PaymentId == paymentId);

            if (p is null)
                throw new InvalidOperationException("payment_not_found");

            if (p.Status != "Success")
                throw new InvalidOperationException("cannot_refund_non_success_payment");

            var refundAmount = p.AmountCents;
            if (refundAmount <= 0)
                throw new InvalidOperationException("nothing_to_refund");

            // 1) Pirma Stripe – kad jei čia nepavyks, DB neliesim
            if (!string.IsNullOrEmpty(p.StripeSessionId))
            {
                await _stripe.RefundAsync(p.StripeSessionId, refundAmount);
            }

            // 2) DB tranzakcija: GiftCard + Payment status
            await using var tx = await _db.Database.BeginTransactionAsync();

            // GiftCard refund – jei buvo naudota
            if (p.GiftCardId is not null && p.GiftCardPlannedCents > 0)
            {
                var ok = await _giftCards.TopUpAsync(p.GiftCardId.Value, p.GiftCardPlannedCents);
                if (!ok)
                    throw new InvalidOperationException("gift_card_refund_failed");
            }

            p.Status = "Refunded";
            // p.RefundedAt = DateTime.UtcNow; // jei turi tokį lauką

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public Task<List<Payment>> GetPaymentsForOrderAsync(int businessId, int orderId)
        {
            return _db.Payments
                .Where(p => p.BusinessId == businessId && p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public Task<List<Payment>> GetPaymentsForBusinessAsync(int businessId)
        {
            return _db.Payments
                .Where(p => p.BusinessId == businessId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}
