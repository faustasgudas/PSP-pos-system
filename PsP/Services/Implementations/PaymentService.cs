using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PsP.Contracts.Payments;
using PsP.Data;
using PsP.Mappings;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IStripePaymentService _stripe;

    public PaymentService(
        AppDbContext db,
        IGiftCardService _ /* unused */,
        IStripePaymentService stripe)
    {
        _db = db;
        _stripe = stripe;
    }

    // ============================================================
    // PUBLIC API
    // ============================================================

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
        var order = await _db.Orders
            .Include(o => o.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct)
            ?? throw new InvalidOperationException("order_not_found");

        if (order.BusinessId != businessId)
            throw new InvalidOperationException("wrong_business");

        if (!string.Equals(order.Status, "Open", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("order_not_open");

        var amountCents = CalculateOrderTotalCents(order);
        if (amountCents <= 0)
            throw new InvalidOperationException("invalid_order_total");

        var tip = tipCents.GetValueOrDefault(0);
        if (tip < 0) throw new InvalidOperationException("tip_invalid");

        var totalCents = checked(amountCents + tip);

        GiftCard? card = null;
        long plannedFromGiftCard = 0;
        long remainingForStripe = totalCents;

        if (!string.IsNullOrWhiteSpace(giftCardCode))
        {
            card = await _db.GiftCards
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == giftCardCode.Trim(), ct)
                ?? throw new InvalidOperationException("invalid_gift_card");

            EnsureActiveAndNotExpired(card);

            if (card.BusinessId != businessId)
                throw new InvalidOperationException("wrong_business");

            var maxFromCard = Math.Min(card.Balance, totalCents);

            plannedFromGiftCard = giftCardAmountCents.HasValue
                ? Math.Min(Math.Max(0, giftCardAmountCents.Value), maxFromCard)
                : maxFromCard;

            remainingForStripe = totalCents - plannedFromGiftCard;
        }

        var method =
            plannedFromGiftCard == 0 ? "Stripe" :
            remainingForStripe == 0 ? "GiftCard" :
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

        if (remainingForStripe > 0)
        {
            var successUrl = $"{baseUrl}/payments/success?sessionId={{CHECKOUT_SESSION_ID}}";
            var cancelUrl  = $"{baseUrl}/payments/cancel?sessionId={{CHECKOUT_SESSION_ID}}";

            var session = _stripe.CreateCheckoutSession(
                remainingForStripe,
                payment.Currency,
                successUrl,
                cancelUrl,
                payment.PaymentId
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

        await FinalizePaymentSuccessAsync(payment.PaymentId, null, ct);
        return new PaymentResponse(payment.PaymentId, plannedFromGiftCard, 0, null, null);
    }

    public async Task ConfirmStripeSuccessAsync(string sessionId, CancellationToken ct = default)
    {
        var p = await _db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StripeSessionId == sessionId, ct);

        if (p != null)
            await FinalizePaymentSuccessAsync(p.PaymentId, sessionId, ct);
    }

    public async Task CancelStripeAsync(string sessionId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var p = await _db.Payments.FirstOrDefaultAsync(x => x.StripeSessionId == sessionId, ct);
        if (p == null || p.Status != "Pending")
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

    // ============================================================
    // PAYMENT FINALIZATION
    // ============================================================

    private async Task FinalizePaymentSuccessAsync(
        int paymentId,
        string? stripeSessionId,
        CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        var p = await _db.Payments.FirstAsync(x => x.PaymentId == paymentId, ct);

        if (stripeSessionId != null && p.StripeSessionId != stripeSessionId)
            throw new InvalidOperationException("stripe_session_mismatch");

        if (p.Status == "Success")
        {
            await tx.CommitAsync(ct);
            return;
        }

        if (p.GiftCardId.HasValue && p.GiftCardPlannedCents > 0 && p.GiftCardChargedCents == 0)
        {
            var card = await _db.GiftCards.FirstAsync(g => g.GiftCardId == p.GiftCardId, ct);
            EnsureActiveAndNotExpired(card);

            var charge = Math.Min(card.Balance, p.GiftCardPlannedCents);
            card.Balance -= charge;
            p.GiftCardChargedCents = charge;
        }

        p.Status = "Success";
        p.IsOpen = false;
        p.CompletedAt = DateTime.UtcNow;

        var order = await _db.Orders.FirstAsync(o => o.OrderId == p.OrderId, ct);
        order.Status = "Closed";

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    // ============================================================
    // TOTAL CALCULATION (CORE FIX)
    // ============================================================

    private static long CalculateOrderTotalCents(Order order)
    {
        var lines = new List<(decimal net, decimal taxRatePct)>();

        foreach (var line in order.Lines)
        {
            if (line.Qty <= 0 || line.UnitPriceSnapshot <= 0) continue;

            var net = Round2(line.UnitPriceSnapshot * line.Qty);
            net = ApplyDiscountFromSnapshotJson(net, line.UnitDiscountSnapshot);
            if (net < 0) net = 0;

            lines.Add((net, Math.Max(0, line.TaxRateSnapshotPct)));
        }

        if (lines.Count == 0) return 0;

        var netSubtotal = lines.Sum(l => l.net);

        var orderType = GetDiscountType(order.OrderDiscountSnapshot);
        var orderValue = GetDiscountValue(order.OrderDiscountSnapshot);

        var discountedNets = new decimal[lines.Count];

        if (orderType == "Percent" && orderValue > 0)
        {
            var factor = Math.Max(0, 1m - orderValue / 100m);
            for (int i = 0; i < lines.Count; i++)
                discountedNets[i] = Round2(lines[i].net * factor);
        }
        else if (orderType == "Amount" && orderValue > 0)
        {
            var remaining = orderValue;

            for (int i = 0; i < lines.Count; i++)
            {
                var share = Round2(orderValue * (lines[i].net / netSubtotal));
                discountedNets[i] = Math.Max(0, lines[i].net - share);
                remaining -= share;
            }

            discountedNets[^1] = Math.Max(0, discountedNets[^1] - remaining);
        }
        else
        {
            for (int i = 0; i < lines.Count; i++)
                discountedNets[i] = lines[i].net;
        }

        decimal taxTotal = 0m;
        decimal finalNet = 0m;

        for (int i = 0; i < lines.Count; i++)
        {
            finalNet += discountedNets[i];
            taxTotal += Round2(discountedNets[i] * lines[i].taxRatePct / 100m);
        }

        return (long)Math.Round((finalNet + taxTotal) * 100m, MidpointRounding.AwayFromZero);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static decimal Round2(decimal v)
        => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private static string? GetDiscountType(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        using var doc = JsonDocument.Parse(json);
        return GetStringCI(doc.RootElement, "type")?.Trim();
    }

    private static decimal GetDiscountValue(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0;
        using var doc = JsonDocument.Parse(json);
        return GetDecimalCI(doc.RootElement, "value");
    }

    private static decimal ApplyDiscountFromSnapshotJson(decimal amount, string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return amount;

        using var doc = JsonDocument.Parse(json);
        var type = GetStringCI(doc.RootElement, "type");
        var value = GetDecimalCI(doc.RootElement, "value");

        return type switch
        {
            "Percent" => amount * (1m - value / 100m),
            "Amount"  => amount - value,
            _ => amount
        };
    }

    private static string? GetStringCI(JsonElement root, string name)
        => root.EnumerateObject()
            .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            .Value.ToString();

    private static decimal GetDecimalCI(JsonElement root, string name)
        => decimal.TryParse(
            GetStringCI(root, name),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var d) ? d : 0m;

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg
           && pg.SqlState == PostgresErrorCodes.UniqueViolation;

    private static void EnsureActiveAndNotExpired(GiftCard c)
    {
        if (!string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("blocked");

        if (c.ExpiresAt != null && c.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("expired");
    }
    public async Task RefundFullAsync(int paymentId, CancellationToken ct = default)
    {
        var p = await _db.Payments
                    .FirstOrDefaultAsync(x => x.PaymentId == paymentId, ct)
                ?? throw new InvalidOperationException("payment_not_found");

        if (p.Status != "Success")
            throw new InvalidOperationException("cannot_refund_non_success_payment");

        var totalCents = p.AmountCents + p.TipCents;

        // Stripe refund
        if (!string.IsNullOrEmpty(p.StripeSessionId))
        {
            var stripeAmount = Math.Max(0, totalCents - p.GiftCardChargedCents);
            if (stripeAmount > 0)
                await _stripe.RefundAsync(p.StripeSessionId, stripeAmount, ct);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        // Refund gift card
        if (p.GiftCardId.HasValue && p.GiftCardChargedCents > 0)
        {
            var card = await _db.GiftCards
                .FirstAsync(g => g.GiftCardId == p.GiftCardId, ct);

            card.Balance += p.GiftCardChargedCents;
        }

        p.Status = "Refunded";
        p.RefundedAt = DateTime.UtcNow;
        p.IsOpen = false;

        var order = await _db.Orders.FirstAsync(o => o.OrderId == p.OrderId, ct);
        order.ApplyRefund();

        
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    
    public Task<List<Payment>> GetPaymentsForOrderAsync(int businessId, int orderId)
    {
        return _db.Payments
            .AsNoTracking()
            .Where(p => p.BusinessId == businessId && p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
    
    public Task<List<Payment>> GetPaymentsForBusinessAsync(int businessId)
    {
        return _db.Payments
            .AsNoTracking()
            .Where(p => p.BusinessId == businessId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
