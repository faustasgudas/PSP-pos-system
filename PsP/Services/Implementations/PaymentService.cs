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


private static long CalculateOrderTotalCents(Order order)
{
    // Helpers
    static long ToCents(decimal amount)
        => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    static decimal FromCents(long cents)
        => cents / 100m;

    static long ClampLong(long v, long min, long max)
        => v < min ? min : (v > max ? max : v);

    static long ApplyDiscountCents(long netCents, string discountSnapshot)
    {
        if (netCents <= 0) return 0;

        var type = GetDiscountType(discountSnapshot);
        var value = GetDiscountValue(discountSnapshot);

        if (string.IsNullOrWhiteSpace(type) || value <= 0) return netCents;

        if (type == "Percent")
        {
            var pct = Math.Max(0m, value);
            var discounted = (decimal)netCents * (1m - pct / 100m);
            return ClampLong(ToCents(discounted / 100m), 0, netCents);
        }

        if (type == "Amount")
        {
            var discCents = Math.Max(0L, ToCents(value));
            return ClampLong(netCents - discCents, 0, netCents);
        }

        return netCents;
    }

    static long CalcTaxCents(long netCents, decimal taxRatePct)
    {
        if (netCents <= 0 || taxRatePct <= 0) return 0;
        var tax = FromCents(netCents) * (taxRatePct / 100m);
        return ToCents(tax);
    }

    var lines = new List<(long NetBeforeOrderCents, decimal TaxRatePct)>();

    foreach (var line in order.Lines)
    {
        if (line.Qty <= 0 || line.UnitPriceSnapshot <= 0) continue;

        var gross = line.UnitPriceSnapshot * line.Qty;      
        var grossCents = ToCents(gross);
        
        var netCents = ApplyDiscountCents(grossCents, line.UnitDiscountSnapshot);

        var taxRate = Math.Max(0m, line.TaxRateSnapshotPct);
        if (netCents > 0)
            lines.Add((netCents, taxRate));
    }

    if (lines.Count == 0) return 0;

    var netSubtotalCents = lines.Sum(l => l.NetBeforeOrderCents);
    if (netSubtotalCents <= 0) return 0;

    var orderType = GetDiscountType(order.OrderDiscountSnapshot);
    var orderValue = GetDiscountValue(order.OrderDiscountSnapshot);

    long orderDiscountTotalCents = 0;

    if (orderValue > 0)
    {
        if (orderType == "Percent")
        {
            var pct = Math.Max(0m, orderValue);
            var disc = FromCents(netSubtotalCents) * (pct / 100m);
            orderDiscountTotalCents = ToCents(disc);
        }
        else if (orderType == "Amount")
        {
            orderDiscountTotalCents = Math.Max(0L, ToCents(orderValue));
        }
    }

    orderDiscountTotalCents = ClampLong(orderDiscountTotalCents, 0, netSubtotalCents);

    var discountedNetCents = new long[lines.Count];
    var discountShareCents = new long[lines.Count];

    if (orderDiscountTotalCents > 0)
    {
        var remainders = new (int idx, long rem)[lines.Count];

        long allocated = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            long net = lines[i].NetBeforeOrderCents;
            
            long raw = checked(orderDiscountTotalCents * net);
            long share = raw / netSubtotalCents;
            long rem = raw % netSubtotalCents;

            share = ClampLong(share, 0, net);

            discountShareCents[i] = share;
            remainders[i] = (i, rem);
            allocated += share;
        }

        long leftover = orderDiscountTotalCents - allocated;
        if (leftover > 0)
        {
            Array.Sort(remainders, (a, b) => b.rem.CompareTo(a.rem));

            for (int k = 0; k < remainders.Length && leftover > 0; k++)
            {
                int i = remainders[k].idx;
                
                if (discountShareCents[i] < lines[i].NetBeforeOrderCents)
                {
                    discountShareCents[i] += 1;
                    leftover -= 1;
                }
            }
        }

        for (int i = 0; i < lines.Count; i++)
        {
            discountedNetCents[i] = lines[i].NetBeforeOrderCents - discountShareCents[i];
            if (discountedNetCents[i] < 0) discountedNetCents[i] = 0;
        }
    }
    else
    {
        for (int i = 0; i < lines.Count; i++)
            discountedNetCents[i] = lines[i].NetBeforeOrderCents;
    }

    long finalNetCents = 0;
    long taxTotalCents = 0;

    for (int i = 0; i < lines.Count; i++)
    {
        finalNetCents += discountedNetCents[i];
        taxTotalCents += CalcTaxCents(discountedNetCents[i], lines[i].TaxRatePct);
    }

    return finalNetCents + taxTotalCents;
}


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

        if (!string.IsNullOrEmpty(p.StripeSessionId))
        {
            var stripeAmount = Math.Max(0, totalCents - p.GiftCardChargedCents);
            if (stripeAmount > 0)
                await _stripe.RefundAsync(p.StripeSessionId, stripeAmount, ct);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        if (p.GiftCardId.HasValue && p.GiftCardChargedCents > 0)
        {
            var card = await _db.GiftCards
                .FirstAsync(g => g.GiftCardId == p.GiftCardId, ct);

            card.Balance += p.GiftCardChargedCents;
        }

        p.Status = "Refunded";
        p.RefundedAt = DateTime.UtcNow;
        p.IsOpen = false;


        
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
