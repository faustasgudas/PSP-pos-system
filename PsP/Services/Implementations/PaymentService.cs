using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PsP.Contracts.Payments;
using PsP.Contracts.Snapshots;
using PsP.Data;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations;

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
        int businessId,
        int callerEmployeeId,
        string? giftCardCode,
        long? giftCardAmountCents,
        long? tipCents,
        string baseUrl,
        CancellationToken ct = default)
    {
        // 1) Order + lines (source of truth)
        var order = await _db.Orders
            .Include(o => o.Lines)
                .ThenInclude(l => l.CatalogItem)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

        if (order is null) throw new InvalidOperationException("order_not_found");
        if (order.BusinessId != businessId) throw new InvalidOperationException("wrong_business");

        if (!string.Equals(order.Status, "Open", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("order_not_open");

        // 2) totals (server-side)
        var amountCents = CalculateOrderTotal(order);
        if (amountCents <= 0) throw new InvalidOperationException("invalid_order_total");

        var tip = tipCents.GetValueOrDefault(0);
        if (tip < 0) throw new InvalidOperationException("tip_invalid");

        var totalCents = checked(amountCents + tip);

        // 3) gift card part
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

            remainingForStripe = totalCents - plannedFromGiftCard;
        }

        var method =
            plannedFromGiftCard == 0 && remainingForStripe > 0 ? "Stripe" :
            plannedFromGiftCard > 0 && remainingForStripe == 0 ? "GiftCard" :
            "GiftCard+Stripe";

        // 4) create Payment row (IsOpen=true => partial unique index enforces one open payment)
        var p = new Payment
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

            InventoryApplied = false,
            InventoryAppliedAt = null,

            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _db.Payments.Add(p);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new InvalidOperationException("payment_already_pending_for_order");
        }

        // 5) Stripe session if needed
        if (remainingForStripe > 0)
        {
            var successUrl = $"{baseUrl}/api/payments/success?sessionId={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{baseUrl}/api/payments/cancel?sessionId={{CHECKOUT_SESSION_ID}}";

            var session = _stripe.CreateCheckoutSession(
                amountCents: remainingForStripe,
                currency: p.Currency,
                successUrl: successUrl,
                cancelUrl: cancelUrl,
                paymentId: p.PaymentId
            );

            p.StripeSessionId = session.Id;
            await _db.SaveChangesAsync(ct);

            return new PaymentResponse(
                p.PaymentId,
                plannedFromGiftCard,
                remainingForStripe,
                session.Url,
                session.Id
            );
        }

        // 6) GiftCard only => confirm now
        await ConfirmGiftCardOnlyAsync(p.PaymentId, ct);

        return new PaymentResponse(p.PaymentId, plannedFromGiftCard, 0, null, null);
    }

    public async Task ConfirmStripeSuccessAsync(string sessionId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var p = await _db.Payments
            .FirstOrDefaultAsync(x => x.StripeSessionId == sessionId, ct);

        if (p is null)
            return;

        // idempotent
        if (p.Status == "Success")
        {
            await tx.CommitAsync(ct);
            return;
        }

        if (p.Status is "Refunded" or "Cancelled")
            throw new InvalidOperationException("payment_not_confirmable");

        // redeem giftcard inside tx (only once)
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

        await ApplySaleInventoryAsync(p, ct);

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

        // order stays open (no stock changes!)
        var order = await _db.Orders.FirstAsync(o => o.OrderId == p.OrderId, ct);
        order.Status = "Open";

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task RefundFullAsync(int paymentId, CancellationToken ct = default)
    {
        var p = await _db.Payments
            .FirstOrDefaultAsync(x => x.PaymentId == paymentId, ct)
            ?? throw new InvalidOperationException("payment_not_found");

        if (p.Status == "Refunded")
            return;

        if (p.Status != "Success")
            throw new InvalidOperationException("cannot_refund_non_success_payment");

        var totalCents = checked(p.AmountCents + p.TipCents);
        if (totalCents <= 0)
            throw new InvalidOperationException("nothing_to_refund");

        // Stripe first (refund only Stripe portion)
        if (!string.IsNullOrEmpty(p.StripeSessionId))
        {
            var stripePortion = Math.Max(0, totalCents - p.GiftCardChargedCents);
            if (stripePortion > 0)
                await _stripe.RefundAsync(p.StripeSessionId, stripePortion, ct);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // GiftCard refund for ACTUAL charged
        if (p.GiftCardId.HasValue && p.GiftCardChargedCents > 0)
        {
            var ok = await _giftCards.TopUpAsync(p.GiftCardId.Value, p.GiftCardChargedCents);
            if (!ok) throw new InvalidOperationException("gift_card_refund_failed");
        }

        await ApplyRefundInventoryAsync(p, ct);

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

        await ApplySaleInventoryAsync(p, ct);

        var order = await _db.Orders.FirstAsync(o => o.OrderId == p.OrderId, ct);
        order.Status = "Closed";

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static long CalculateOrderTotal(Order order)
    {
        decimal subtotal = 0m;

        foreach (var line in order.Lines)
        {
            if (line.UnitPriceSnapshot <= 0)
                throw new InvalidOperationException("missing_price_snapshot");

            var lineGross = line.UnitPriceSnapshot * line.Qty;
            var lineNet = ApplyLineDiscount(lineGross, line.UnitDiscountSnapshot);

            if (lineNet < 0)
                lineNet = 0;

            subtotal += lineNet;
        }

        var totalAfterOrderDiscount =
            ApplyOrderDiscount(subtotal, order.OrderDiscountSnapshot);

        if (totalAfterOrderDiscount < 0)
            totalAfterOrderDiscount = 0;

        // EUR -> cents
        return (long)Math.Round(
            totalAfterOrderDiscount * 100m,
            MidpointRounding.AwayFromZero);
    }


    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg
           && pg.SqlState == PostgresErrorCodes.UniqueViolation;

    private async Task ApplySaleInventoryAsync(Payment p, CancellationToken ct)
    {
        if (p.InventoryApplied)
            return;

        var order = await _db.Orders
            .Include(o => o.Lines)
                .ThenInclude(l => l.CatalogItem)
            .FirstAsync(o => o.OrderId == p.OrderId, ct);

        foreach (var line in order.Lines)
        {
            var item = line.CatalogItem;
            if (item is null) continue;

            if (!string.Equals(item.Type, "product", StringComparison.OrdinalIgnoreCase))
                continue;

            var stockItem = await _db.StockItems
                .FirstOrDefaultAsync(s => s.CatalogItemId == item.CatalogItemId, ct)
                ?? throw new InvalidOperationException("stock_item_missing");

            
        }

        p.InventoryApplied = true;
        p.InventoryAppliedAt = DateTime.UtcNow;
    }

    private async Task ApplyRefundInventoryAsync(Payment p, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
                .ThenInclude(l => l.CatalogItem)
            .FirstAsync(o => o.OrderId == p.OrderId, ct);

        foreach (var line in order.Lines)
        {
            var item = line.CatalogItem;
            if (item is null) continue;

            if (!string.Equals(item.Type, "product", StringComparison.OrdinalIgnoreCase))
                continue;

            var stockItem = await _db.StockItems
                .FirstOrDefaultAsync(s => s.CatalogItemId == item.CatalogItemId, ct)
                ?? throw new InvalidOperationException("stock_item_missing");

            
        }
    }
    
    private static decimal ApplyLineDiscount(
        decimal lineGross,
        string? discountSnapshotJson)
    {
        if (string.IsNullOrWhiteSpace(discountSnapshotJson))
            return lineGross;

        var snap = ParseDiscountSnapshot(discountSnapshotJson);
        if (snap is null)
            return lineGross;

        return snap.Type switch
        {
            "Percent" => lineGross * (1m - snap.Value / 100m),
            "Amount"  => lineGross - snap.Value,
            _         => lineGross
        };
    }

    private static decimal ApplyOrderDiscount(
        decimal subtotal,
        string? discountSnapshotJson)
    {
        if (string.IsNullOrWhiteSpace(discountSnapshotJson))
            return subtotal;

        var snap = ParseDiscountSnapshot(discountSnapshotJson);
        if (snap is null)
            return subtotal;

        return snap.Type switch
        {
            "Percent" => subtotal * (1m - snap.Value / 100m),
            "Amount"  => subtotal - snap.Value,
            _         => subtotal
        };
    }
    private static DiscountSnapshot? ParseDiscountSnapshot(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DiscountSnapshot>(json);
        }
        catch
        {
            return null;
        }
    }

}
