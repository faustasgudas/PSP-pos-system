using PsP.Services.Interfaces;

namespace PsP.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Orders;
using PsP.Data;
using PsP.Mappings;
using PsP.Models;

public class OrdersService : IOrdersService
{
    private readonly AppDbContext _db;
    private readonly IDiscountsService _discounts;

    public OrdersService(AppDbContext db, IDiscountsService discounts)
    {
        _db = db;
        _discounts = discounts;
    }

    private static bool IsManagerOrOwner(Employee e)
        => string.Equals(e.Role, "Owner", StringComparison.OrdinalIgnoreCase)
           || string.Equals(e.Role, "Manager", StringComparison.OrdinalIgnoreCase);

    private async Task<Employee> GetCallerAsync(int businessId, int callerEmployeeId, CancellationToken ct)
    {
        var caller = await _db.Employees
                         .AsNoTracking()
                         .FirstOrDefaultAsync(x => x.BusinessId == businessId && x.EmployeeId == callerEmployeeId, ct)
                     ?? throw new InvalidOperationException("Caller employee not found in this business.");

        if (!string.Equals(caller.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Caller is not active.");

        return caller;
    }

    private async Task<Order> GetOrderEntityAsync(int businessId, int orderId, CancellationToken ct)
    {
        return await _db.Orders
                   .FirstOrDefaultAsync(o => o.BusinessId == businessId && o.OrderId == orderId, ct)
               ?? throw new InvalidOperationException("Order not found.");
    }


    private static void EnsureOpen(Order o)
    {
        if (!string.Equals(o.Status, "Open", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Operation allowed only for OPEN orders.");
    }


    private static void EnsureCallerCanSeeOrder(Employee caller, Order o)
    {
        if (IsManagerOrOwner(caller)) return;

        if (o.EmployeeId != caller.EmployeeId)
            throw new InvalidOperationException("Forbidden: staff can only access their own orders.");
    }

    private static decimal CalcUnitDiscount(decimal unitPrice, Discount d)
    {
        // 2dp rounding — consistent with money
        if (d.Type == "Percent")
            return Math.Round(unitPrice * d.Value / 100m, 2, MidpointRounding.AwayFromZero);
        // "Amount"
        return Math.Round(d.Value, 2, MidpointRounding.AwayFromZero);
    }



    private async Task<decimal> ResolveTaxRatePctAsync(Business business, string taxClass, CancellationToken ct)
    {
        // pick a currently valid rule if present; if none, 0
        var now = DateTime.UtcNow;
        var rule = await _db.TaxRules
            .AsNoTracking()
            .Where(tr => tr.CountryCode == business.CountryCode
                         && tr.TaxClass == taxClass
                         && tr.ValidFrom <= now && tr.ValidTo >= now)
            .OrderByDescending(tr => tr.ValidFrom)
            .FirstOrDefaultAsync(ct);

        return rule?.RatePercent ?? 0m;
    }






    public async Task<IEnumerable<OrderSummaryResponse>> ListAllAsync(
        int businessId,
        int callerEmployeeId,
        string? status,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);
        if (!IsManagerOrOwner(caller))
            throw new InvalidOperationException("Forbidden: only managers/owners can list all orders.");

        var q = _db.Orders.AsNoTracking().Where(o => o.BusinessId == businessId);

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(o => o.Status == status);

        if (from.HasValue) q = q.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(o => o.CreatedAt <= to.Value);

        var orders = await q
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return orders.ToSummaryResponses();
    }

    public async Task<IEnumerable<OrderSummaryResponse>> ListMineAsync(
        int businessId,
        int callerEmployeeId,
        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);

        // Staff: only own orders; Managers/Owners: also own (by design of this endpoint)
        var q = _db.Orders.AsNoTracking()
            .Where(o => o.BusinessId == businessId && o.EmployeeId == caller.EmployeeId);

        // Usually “mine” shows current work => only Open by default.
        q = q.Where(o => o.Status == "Open");

        var orders = await q.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
        return orders.ToSummaryResponses();
    }

    public async Task<OrderDetailResponse> GetOrderAsync(
        int businessId,
        int orderId,
        int callerEmployeeId,
        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);
        var order = await GetOrderEntityAsync(businessId, orderId, ct);

        EnsureCallerCanSeeOrder(caller, order);

        var lines = await _db.OrderLines
            .AsNoTracking()
            .Where(l => l.BusinessId == businessId && l.OrderId == orderId)
            .OrderBy(l => l.OrderLineId)
            .ToListAsync(ct);

        return order.ToDetailResponse(lines);
    }

    public async Task<IEnumerable<OrderLineResponse>> ListLinesAsync(
        int businessId,
        int orderId,
        int callerEmployeeId,
        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(caller, order);

        var lines = await _db.OrderLines
            .AsNoTracking()
            .Where(l => l.BusinessId == businessId && l.OrderId == orderId)
            .OrderBy(l => l.OrderLineId)
            .ToListAsync(ct);

        return lines.ToLineResponses();
    }

    public async Task<OrderLineResponse> GetLineAsync(
        int businessId,
        int orderId,
        int orderLineId,
        int callerEmployeeId,
        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(caller, order);

        var line = await _db.OrderLines
                       .AsNoTracking()
                       .FirstOrDefaultAsync(
                           l => l.BusinessId == businessId && l.OrderId == orderId && l.OrderLineId == orderLineId, ct)
                   ?? throw new InvalidOperationException("Order line not found.");

        return line.ToLineResponse();
    }

    public async Task<OrderDetailResponse> CreateOrderAsync(
        int businessId,
        CreateOrderRequest request,

        CancellationToken ct = default)
    {
        // validate caller exists & belongs to business
        _ = await GetCallerAsync(businessId, request.EmployeeId, ct);



        if (request.ReservationId.HasValue)
        {
            var reservation = await _db.Reservations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ReservationId == request.ReservationId.Value && r.BusinessId == businessId,
                    ct);

            if (reservation == null)
                throw new InvalidOperationException("Reservation not found for this business.");
        }


        Order entity;
        var discount = await _discounts.GetNewestOrderDiscountAsync(businessId, null, ct);
        if (discount == null)
        {
            entity = request.ToNewEntity(businessId, null, null);
        }
        else
        {
            var discountSnapShot = _discounts.MakeOrderDiscountSnapshot(discount);
            entity = request.ToNewEntity(businessId, discount.DiscountId, discountSnapShot);

        }



        _db.Orders.Add(entity);
        await _db.SaveChangesAsync(ct);

        // empty lines for fresh order
        return entity.ToDetailResponse(Enumerable.Empty<OrderLine>());
    }

    public async Task<OrderDetailResponse> UpdateOrderAsync(
        int businessId,
        int orderId,
        int callerEmployeeId,
        UpdateOrderRequest request,

        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(caller, order);
        EnsureOpen(order);

        string? snapshot = null;
        if (request.DiscountId.HasValue)
        {
            var discount =
                await _discounts.EnsureOrderDiscountEligibleAsync(businessId, request.DiscountId.Value, null, ct);
            snapshot = _discounts.MakeOrderDiscountSnapshot(discount);
        }



        request.ApplyUpdate(order);
        order.OrderDiscountSnapshot = snapshot;



        await _db.SaveChangesAsync(ct);
        var lines = await _db.OrderLines
            .AsNoTracking()
            .Where(l => l.BusinessId == businessId && l.OrderId == orderId)
            .ToListAsync(ct);

        return order.ToDetailResponse(lines);
    }

    public async Task<OrderDetailResponse> CloseOrderAsync(
        int businessId,
        int orderId,
        int callerEmployeeId,
        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(caller, order);
        EnsureOpen(order);

        order.ApplyClose();
        await _db.SaveChangesAsync(ct);

        var lines = await _db.OrderLines
            .AsNoTracking()
            .Where(l => l.BusinessId == businessId && l.OrderId == orderId)
            .ToListAsync(ct);

        return order.ToDetailResponse(lines);
    }

    public async Task<OrderDetailResponse> CancelOrderAsync(
        int businessId,
        int orderId,
        int callerEmployeeId,
        CancelOrderRequest request,
        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(caller, order);
        EnsureOpen(order);

        order.ApplyCancel();
        // If you need to persist reason: put into a separate audit table or log.
        await _db.SaveChangesAsync(ct);

        var lines = await _db.OrderLines
            .AsNoTracking()
            .Where(l => l.BusinessId == businessId && l.OrderId == orderId)
            .ToListAsync(ct);

        return order.ToDetailResponse(lines);
    }

    public async Task<OrderLineResponse> AddLineAsync(
        int businessId,
        int orderId,
        int callerEmployeeId,
        AddLineRequest request,

        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(caller, order);
        EnsureOpen(order);

        // Resolve snapshots from CatalogItem (+ tax)
        var item = await _db.CatalogItems
                       .AsNoTracking()
                       .FirstOrDefaultAsync(
                           ci => ci.BusinessId == businessId && ci.CatalogItemId == request.CatalogItemId, ct)
                   ?? throw new InvalidOperationException("Catalog item not found in this business.");

        var business = await _db.Businesses
            .AsNoTracking()
            .FirstAsync(b => b.BusinessId == businessId, ct);

        var taxRate = await ResolveTaxRatePctAsync(business, item.TaxClass, ct);

        var discount = await _discounts.GetNewestLineDiscountForItemAsync(businessId, request.CatalogItemId, null, ct);

        string? snapshot = null;

        if (discount != null)
        {
            snapshot = _discounts.MakeLineDiscountSnapshot(discount, request.CatalogItemId, null);
        }

        var line = request.ToNewLineEntity(
            businessId: businessId,
            orderId: orderId,
            performedByEmployeeId: callerEmployeeId,
            itemNameSnapshot: item.Name,
            unitPriceSnapshot: item.BasePrice,
            taxClassSnapshot: item.TaxClass,
            taxRateSnapshotPct: taxRate,
            unitDiscountSnapshot: snapshot,
            nowUtc: DateTime.UtcNow
        );

        _db.OrderLines.Add(line);
        await _db.SaveChangesAsync(ct);

        return line.ToLineResponse();
    }

    public async Task<OrderLineResponse> UpdateLineAsync(
        int businessId,
        int orderId,
        int orderLineId,
        int callerEmployeeId,
        UpdateLineRequest request,

        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(caller, order);
        EnsureOpen(order);



        var line = await _db.OrderLines
                       .FirstOrDefaultAsync(
                           l => l.BusinessId == businessId && l.OrderId == orderId && l.OrderLineId == orderLineId, ct)
                   ?? throw new InvalidOperationException("Order line not found.");

        string? refreshedDiscountSnapshot = null;
        if (request.DiscountId.HasValue)
        {
            var discount = await _discounts.EnsureLineDiscountEligibleAsync(businessId, (int)request.DiscountId,
                line.CatalogItemId, null, ct);
            refreshedDiscountSnapshot = _discounts.MakeLineDiscountSnapshot(discount, line.CatalogItemId);
        }

        request.ApplyUpdate(line, performedByEmployeeId: callerEmployeeId, nowUtc: DateTime.UtcNow,
            unitDiscountSnapshot: refreshedDiscountSnapshot);

        await _db.SaveChangesAsync(ct);

        return line.ToLineResponse();
    }

    public async Task RemoveLineAsync(
        int businessId,
        int orderId,
        int orderLineId,
        int callerEmployeeId,
        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(caller, order);
        EnsureOpen(order);

        var line = await _db.OrderLines
                       .FirstOrDefaultAsync(
                           l => l.BusinessId == businessId && l.OrderId == orderId && l.OrderLineId == orderLineId, ct)
                   ?? throw new InvalidOperationException("Order line not found.");

        _db.OrderLines.Remove(line);
        await _db.SaveChangesAsync(ct);
    }






}
    