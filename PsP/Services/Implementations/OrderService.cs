using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Orders;
using PsP.Data;
using PsP.Mappings;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations;

public class OrdersService : IOrdersService
{
    private readonly AppDbContext _db;
    private readonly IDiscountsService _discounts;

    public OrdersService(AppDbContext db, IDiscountsService discounts)
    {
        _db = db;
        _discounts = discounts;
    }

    private static bool IsManagerOrOwner(string role)
        => string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase)
           || string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);

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

    private static void EnsureCallerCanSeeOrder(int callerEmployeeId, string callerRole, Order o)
    {
        if (IsManagerOrOwner(callerRole)) return;

        if (o.EmployeeId != callerEmployeeId)
            throw new InvalidOperationException("Forbidden: staff can only access their own orders.");
    }

    private async Task<decimal> ResolveTaxRatePctAsync(Business business, string taxClass, CancellationToken ct)
    {
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

    // ========== QUERIES ==========

    public async Task<IEnumerable<OrderSummaryResponse>> ListAllAsync(
        int businessId,
        int callerEmployeeId,
        string callerRole,
        string? status,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        if (!IsManagerOrOwner(callerRole))
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
        string callerRole,
        CancellationToken ct = default)
    {
        var q = _db.Orders.AsNoTracking()
            .Where(o => o.BusinessId == businessId && o.EmployeeId == callerEmployeeId)
            .Where(o => o.Status == "Open");

        var orders = await q
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return orders.ToSummaryResponses();
    }

    public async Task<OrderDetailResponse> GetOrderAsync(
        int businessId,
        int orderId,
        int callerEmployeeId,
        string callerRole,
        CancellationToken ct = default)
    {
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(callerEmployeeId, callerRole, order);

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
        string callerRole,
        CancellationToken ct = default)
    {
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(callerEmployeeId, callerRole, order);

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
        string callerRole,
        CancellationToken ct = default)
    {
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(callerEmployeeId, callerRole, order);

        var line = await _db.OrderLines
                       .AsNoTracking()
                       .FirstOrDefaultAsync(
                           l => l.BusinessId == businessId &&
                                l.OrderId == orderId &&
                                l.OrderLineId == orderLineId, ct)
                   ?? throw new InvalidOperationException("Order line not found.");

        return line.ToLineResponse();
    }

    // ========== ORDER HEADER COMMANDS ==========

    public async Task<OrderDetailResponse> CreateOrderAsync(
        int businessId,
        int callerEmployeeId,
        string callerRole,
        CreateOrderRequest request,
        CancellationToken ct = default)
    {
        // visada priskiriam orderį JWT employee
        request.EmployeeId = callerEmployeeId;

        if (request.ReservationId.HasValue)
        {
            var reservation = await _db.Reservations
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                        r.ReservationId == request.ReservationId.Value &&
                        r.BusinessId == businessId,
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

        return entity.ToDetailResponse(Enumerable.Empty<OrderLine>());
    }

    public async Task<OrderDetailResponse> UpdateOrderAsync(
        int businessId,
        int orderId,
        int callerEmployeeId,
        string callerRole,
        UpdateOrderRequest request,
        CancellationToken ct = default)
    {
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(callerEmployeeId, callerRole, order);
        EnsureOpen(order);

        string? snapshot = null;
        if (request.DiscountId.HasValue)
        {
            var discount = await _discounts.EnsureOrderDiscountEligibleAsync(
                businessId,
                request.DiscountId.Value,
                null,
                ct);

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
        string callerRole,
        CancellationToken ct = default)
    {
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(callerEmployeeId, callerRole, order);
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
        string callerRole,
        CancelOrderRequest request,
        CancellationToken ct = default)
    {
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(callerEmployeeId, callerRole, order);
        EnsureOpen(order);

        order.ApplyCancel();
        await _db.SaveChangesAsync(ct);

        var lines = await _db.OrderLines
            .AsNoTracking()
            .Where(l => l.BusinessId == businessId && l.OrderId == orderId)
            .ToListAsync(ct);

        return order.ToDetailResponse(lines);
    }

    // ========== LINES COMMANDS ==========

    public async Task<OrderLineResponse> AddLineAsync(
        int businessId,
        int orderId,
        int callerEmployeeId,
        string callerRole,
        AddLineRequest request,
        CancellationToken ct = default)
    {
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(callerEmployeeId, callerRole, order);
        EnsureOpen(order);

        var item = await _db.CatalogItems
                       .AsNoTracking()
                       .FirstOrDefaultAsync(
                           ci => ci.BusinessId == businessId &&
                                 ci.CatalogItemId == request.CatalogItemId, ct)
                   ?? throw new InvalidOperationException("Catalog item not found in this business.");

        var business = await _db.Businesses
            .AsNoTracking()
            .FirstAsync(b => b.BusinessId == businessId, ct);

        var taxRate = await ResolveTaxRatePctAsync(business, item.TaxClass, ct);

        var discount = await _discounts.GetNewestLineDiscountForItemAsync(
            businessId,
            request.CatalogItemId,
            null,
            ct);

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
        string callerRole,
        UpdateLineRequest request,
        CancellationToken ct = default)
    {
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(callerEmployeeId, callerRole, order);
        EnsureOpen(order);

        var line = await _db.OrderLines
                       .FirstOrDefaultAsync(
                           l => l.BusinessId == businessId &&
                                l.OrderId == orderId &&
                                l.OrderLineId == orderLineId, ct)
                   ?? throw new InvalidOperationException("Order line not found.");

        string? refreshedDiscountSnapshot = null;
        if (request.DiscountId.HasValue)
        {
            var discount = await _discounts.EnsureLineDiscountEligibleAsync(
                businessId,
                request.DiscountId.Value,
                line.CatalogItemId,
                null,
                ct);

            refreshedDiscountSnapshot = _discounts.MakeLineDiscountSnapshot(
                discount,
                line.CatalogItemId);
        }

        request.ApplyUpdate(
            line,
            performedByEmployeeId: callerEmployeeId,
            nowUtc: DateTime.UtcNow,
            unitDiscountSnapshot: refreshedDiscountSnapshot);

        await _db.SaveChangesAsync(ct);

        return line.ToLineResponse();
    }

    public async Task RemoveLineAsync(
        int businessId,
        int orderId,
        int orderLineId,
        int callerEmployeeId,
        string callerRole,
        CancellationToken ct = default)
    {
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(callerEmployeeId, callerRole, order);
        EnsureOpen(order);

        var line = await _db.OrderLines
                       .FirstOrDefaultAsync(
                           l => l.BusinessId == businessId &&
                                l.OrderId == orderId &&
                                l.OrderLineId == orderLineId, ct)
                   ?? throw new InvalidOperationException("Order line not found.");

        _db.OrderLines.Remove(line);
        await _db.SaveChangesAsync(ct);
    }
}
