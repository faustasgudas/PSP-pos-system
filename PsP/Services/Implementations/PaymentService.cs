using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PsP.Contracts.Payments;
using PsP.Data;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IGiftCardService _giftCards;
    private readonly IStripePaymentService _stripe;

    public PaymentService(AppDbContext db, IGiftCardService giftCards, IStripePaymentService stripe)
    {
        _db = db;
        _giftCards = giftCards;
        _stripe = stripe;
    }

    public async Task<PaymentResponse> CreatePaymentAsync(
        int orderId,
        int businessId,
        int callerEmployeeId,
        string? giftCardCode,
        long? giftCardAmountCents,
        long? tipCents,
        string baseUrl,
        CancellationToken ct = default)
    {
        // Load order with lines & snapshots
        var order = await _db.Orders
            .Include(o => o.Lines)
                .ThenInclude(l => l.CatalogItem)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

        if (order is null) throw new InvalidOperationException("order_not_found");
        if (order.BusinessId != businessId) throw new InvalidOperationException("wrong_business");
        if (!string.Equals(order.Status, "Open", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("order_not_open");

        var amountCents = CalculateOrderTotalCents(order);
        if (amountCents <= 0) throw new InvalidOperationException("invalid_order_total");

        var tip = tipCents.GetValueOrDefault(0);
        if (tip < 0) throw new InvalidOperationException("tip_invalid");

        var totalCents = checked(amountCents + tip);

        GiftCard? card = null;
        long plannedFromGiftCard = 0;
        long remainingForStripe = totalCents;

        if (!string.IsNullOrWhiteSpace(giftCardCode))
        {
            card = await _giftCards.GetByCodeAsync(giftCardCode.Trim())
                   ?? throw new InvalidOperationException("invalid_gift_card");

            if (card.BusinessId != businessId) throw new InvalidOperationException("wrong_business");
            if (!string.Equals(card.Status, "Active", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("blocked");
            if (card.ExpiresAt is not null && card.ExpiresAt <= DateTime.UtcNow)
                throw new InvalidOperationException("expired");

            var maxFromCard = Math.Min(card.Balance, totalCents);

            plannedFromGiftCard = giftCardAmountCents.HasValue
                ? Math.Min(Math.Max(0, giftCardAmountCents.Value), maxFromCard)
                : maxFromCard;

            remainingForStripe = totalCents - plannedFromGiftCard;
        }

        var method =
            plannedFromGiftCard == 0 && remainingForStripe > 0 ? "Stripe" :
            plannedFromGiftCard > 0 && remainingForStripe == 0 ? "GiftCard" :
            "GiftCard+Stripe";

        var payment = new Payment
        {
            BusinessId = businessId,
            OrderId = orderId,
            EmployeeId = callerEmployeeId,

            AmountCents = amountCents,
            TipCents = tip,

            Currency = "EUR",
            Method = method,
            Status = "Pending",
            IsOpen = true,

            GiftCardId = plannedFromGiftCard > 0 ? card?.GiftCardId : null,
            GiftCardPlannedCents = plannedFromGiftCard,
            GiftCardChargedCents = 0,

            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new InvalidOperationException("payment_already_pending_for_order");
        }

        // If Stripe portion exists -> create checkout session
        if (remainingForStripe > 0)
        {
            // NOTE: rekomenduoju iš config/appsettings (frontend url), bet palieku tavo logiką
            var clientUrl = "http://localhost:5173";

            var successUrl = $"{clientUrl}/payments/success?sessionId={{CHECKOUT_SESSION_ID}}";
            var cancelUrl  = $"{clientUrl}/payments/cancel?sessionId={{CHECKOUT_SESSION_ID}}";

            var session = _stripe.CreateCheckoutSession(
                amountCents: remainingForStripe,
                currency: payment.Currency,
                successUrl: successUrl,
                cancelUrl: cancelUrl,
                paymentId: payment.PaymentId
            );

            payment.StripeSessionId = session.Id;
            await _db.SaveChangesAsync(ct);

            return new PaymentResponse(
                payment.PaymentId,
                plannedFromGiftCard,
                remainingForStripe,
                session.Url,
                session.Id
            );
        }

        // GiftCard-only: confirm immediately
        await ConfirmGiftCardOnlyAsync(payment.PaymentId, ct);
        return new PaymentResponse(payment.PaymentId, plannedFromGiftCard, 0, null, null);
    }

    public async Task ConfirmStripeSuccessAsync(string sessionId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var p = await _db.Payments.FirstOrDefaultAsync(x => x.StripeSessionId == sessionId, ct);
        if (p is null)
        {
            await tx.CommitAsync(ct);
            return;
        }

        if (p.Status == "Success")
        {
            await tx.CommitAsync(ct);
            return;
        }

        if (p.Status is "Refunded" or "Cancelled")
            throw new InvalidOperationException("payment_not_confirmable");

        // Redeem gift card if planned and not yet charged
        if (p.GiftCardId.HasValue && p.GiftCardPlannedCents > 0 && p.GiftCardChargedCents == 0)
        {
            var (charged, _) = await _giftCards.RedeemAsync(
                p.GiftCardId.Value,
                p.GiftCardPlannedCents,
                p.BusinessId
            );
            p.GiftCardChargedCents = charged;
        }

        p.Status = "Success";
        p.CompletedAt = DateTime.UtcNow;
        p.IsOpen = false;

        var order = await _db.Orders.FirstAsync(o => o.OrderId == p.OrderId, ct);
        order.Status = "Closed";

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task CancelStripeAsync(string sessionId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var p = await _db.Payments.FirstOrDefaultAsync(x => x.StripeSessionId == sessionId, ct);
        if (p is null)
        {
            await tx.CommitAsync(ct);
            return;
        }

        if (p.Status != "Pending")
        {
            await tx.CommitAsync(ct);
            return;
        }

        p.Status = "Cancelled";
        p.IsOpen = false;

        var order = await _db.Orders.FirstAsync(o => o.OrderId == p.OrderId, ct);
        order.Status = "Open";

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task RefundFullAsync(int paymentId, CancellationToken ct = default)
    {
        var p = await _db.Payments.FirstOrDefaultAsync(x => x.PaymentId == paymentId, ct)
            ?? throw new InvalidOperationException("payment_not_found");

        if (p.Status == "Refunded")
            return;

        if (p.Status != "Success")
            throw new InvalidOperationException("cannot_refund_non_success_payment");

        var totalCents = checked(p.AmountCents + p.TipCents);
        if (totalCents <= 0)
            throw new InvalidOperationException("nothing_to_refund");

        // Stripe refund first (outside DB tx)
        if (!string.IsNullOrEmpty(p.StripeSessionId))
        {
            var stripePortion = Math.Max(0, totalCents - p.GiftCardChargedCents);
            if (stripePortion > 0)
                await _stripe.RefundAsync(p.StripeSessionId, stripePortion, ct);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Refund gift card portion
        if (p.GiftCardId.HasValue && p.GiftCardChargedCents > 0)
        {
            var ok = await _giftCards.TopUpAsync(p.GiftCardId.Value, p.GiftCardChargedCents);
            if (!ok) throw new InvalidOperationException("gift_card_refund_failed");
        }

        p.Status = "Refunded";
        p.RefundedAt = DateTime.UtcNow;
        p.IsOpen = false;

        var order = await _db.Orders.FirstAsync(o => o.OrderId == p.OrderId, ct);
        order.Status = "Cancelled";

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public Task<List<Payment>> GetPaymentsForOrderAsync(int businessId, int orderId)
        => _db.Payments.AsNoTracking()
            .Where(p => p.BusinessId == businessId && p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public Task<List<Payment>> GetPaymentsForBusinessAsync(int businessId)
        => _db.Payments.AsNoTracking()
            .Where(p => p.BusinessId == businessId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    // -------------------- Internals --------------------

    private async Task ConfirmGiftCardOnlyAsync(int paymentId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var p = await _db.Payments.FirstOrDefaultAsync(x => x.PaymentId == paymentId, ct)
                ?? throw new InvalidOperationException("payment_not_found");

        if (p.Status == "Success")
        {
            await tx.CommitAsync(ct);
            return;
        }

        if (p.Status is "Refunded" or "Cancelled")
            throw new InvalidOperationException("payment_not_confirmable");

        if (p.GiftCardId.HasValue && p.GiftCardPlannedCents > 0 && p.GiftCardChargedCents == 0)
        {
            var (charged, _) = await _giftCards.RedeemAsync(
                p.GiftCardId.Value,
                p.GiftCardPlannedCents,
                p.BusinessId);

            p.GiftCardChargedCents = charged;
        }

        p.Status = "Success";
        p.CompletedAt = DateTime.UtcNow;
        p.IsOpen = false;

        var order = await _db.Orders.FirstAsync(o => o.OrderId == p.OrderId, ct);
        order.Status = "Closed";

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg
           && pg.SqlState == PostgresErrorCodes.UniqueViolation;

    /// <summary>
    /// Calculates order total in cents. Uses snapshots (unit price + line discount snapshot + order discount snapshot).
    /// IMPORTANT: line.UnitPriceSnapshot and discount "Amount" must be in same money unit (e.g. EUR).
    /// </summary>
    private static long CalculateOrderTotalCents(Order order)
    {
        decimal subtotal = 0m;

        foreach (var line in order.Lines)
        {
            if (line.UnitPriceSnapshot <= 0)
                throw new InvalidOperationException("missing_price_snapshot");

            var lineGross = line.UnitPriceSnapshot * line.Qty;

            var lineNet = ApplyDiscountFromSnapshotJson(
                amount: lineGross,
                discountSnapshotJson: line.UnitDiscountSnapshot);

            if (lineNet < 0) lineNet = 0;
            subtotal += lineNet;
        }

        var total = ApplyDiscountFromSnapshotJson(
            amount: subtotal,
            discountSnapshotJson: order.OrderDiscountSnapshot);

        if (total < 0) total = 0;

        // Convert EUR -> cents
        return (long)Math.Round(total * 100m, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Applies discount using snapshot JSON WITHOUT DTO deserialization.
    /// Handles camelCase/PascalCase by case-insensitive property matching.
    /// Expected JSON fields: type, value (string/number). type: "Percent"|"Amount".
    /// </summary>
    private static decimal ApplyDiscountFromSnapshotJson(decimal amount, string? discountSnapshotJson)
    {
        if (string.IsNullOrWhiteSpace(discountSnapshotJson))
            return amount;

        try
        {
            using var doc = JsonDocument.Parse(discountSnapshotJson);
            var root = doc.RootElement;

            var type = GetStringCI(root, "type");
            var value = GetDecimalCI(root, "value");

            if (string.IsNullOrWhiteSpace(type) || value <= 0m)
                return amount;

            // normalize type (optional)
            type = type.Trim();

            return type switch
            {
                "Percent" => amount * (1m - (value / 100m)),
                "Amount"  => amount - value,
                _         => amount
            };
        }
        catch
        {
            // If snapshot is garbage -> ignore discount (fail-safe)
            return amount;
        }
    }

    private static string? GetStringCI(JsonElement root, string name)
    {
        foreach (var p in root.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                if (p.Value.ValueKind == JsonValueKind.String)
                    return p.Value.GetString();
                return p.Value.ToString();
            }
        }
        return null;
    }

    private static decimal GetDecimalCI(JsonElement root, string name)
    {
        foreach (var p in root.EnumerateObject())
        {
            if (!string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;

            return p.Value.ValueKind switch
            {
                JsonValueKind.Number => p.Value.GetDecimal(),
                JsonValueKind.String => decimal.TryParse(
                        p.Value.GetString(),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var d)
                    ? d
                    : 0m,
                _ => 0m
            };
        }

        return 0m;
    }
}
