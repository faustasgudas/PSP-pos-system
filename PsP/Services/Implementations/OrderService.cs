using PsP.Contracts.StockMovements;
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
    private readonly IStockMovementService _stockMovement;
    public OrdersService(AppDbContext db, IDiscountsService discounts,IStockMovementService stockMovement)
    {
        _db = db;
        _discounts = discounts;
        _stockMovement = stockMovement;
    }

    private static bool IsManagerOrOwner(Employee e)
        => string.Equals(e.Role, "owner", StringComparison.OrdinalIgnoreCase)
           || string.Equals(e.Role, "manager", StringComparison.OrdinalIgnoreCase);

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
    
    private async Task<Employee> EnsureActiveEmployeeBelongsToBusinessAsync(int businessId, int callerEmployeeId, CancellationToken ct)
    {
        var caller = await _db.Employees
                         .AsNoTracking()
                         .FirstOrDefaultAsync(x => x.BusinessId == businessId && x.EmployeeId == callerEmployeeId, ct)
                     ?? throw new InvalidOperationException("Employee not found in this business.");

        if (!string.Equals(caller.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Employee is not active.");

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
            .OrderByDescending(r => r.ValidFrom)  
            .ThenByDescending(r => r.TaxRuleId)
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
        int callerEmployeeId,
        CreateOrderRequest request,

        CancellationToken ct = default)
    {
        // validate caller exists & belongs to business
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);

        if (callerEmployeeId != request.EmployeeId)
        {
            var employee = await EnsureActiveEmployeeBelongsToBusinessAsync(businessId, request.EmployeeId, ct);
            if (!IsManagerOrOwner(caller))
            {
                throw new InvalidOperationException("Forbidden: only managers/owners can create order for others.");
            }
        }


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
        
        if (callerEmployeeId != request.EmployeeId)
        {
            _ = await EnsureActiveEmployeeBelongsToBusinessAsync(businessId, request.EmployeeId, ct);
            if (!IsManagerOrOwner(caller))
            {
                throw new InvalidOperationException("Forbidden: only managers/owners can update order for others.");
            }
            
        }
        
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

        
        
        var lines = await _db.OrderLines
            .Where(l => l.OrderId == orderId && l.BusinessId == businessId)
            .ToListAsync(ct);

        foreach (var line in lines)
        {
            var item = await _db.CatalogItems
                           .AsNoTracking()
                           .FirstOrDefaultAsync(
                               ci => ci.BusinessId == businessId && ci.CatalogItemId == line.CatalogItemId, ct)
                       ?? throw new InvalidOperationException("Catalog item not found in this business.");
            
            // Only return stock for products, not services
            if (string.Equals(item.Type, "Product", StringComparison.OrdinalIgnoreCase))
            {
                var stockItem = await _db.StockItems
                    .FirstOrDefaultAsync(s => s.CatalogItemId == line.CatalogItemId, ct);
                
                if (stockItem != null)
                {
                    await _stockMovement.CreateAsync(
                        businessId,
                        stockItem.StockItemId,
                        callerEmployeeId,
                        new CreateStockMovementRequest
                        {
                            Type = "Adjust",
                            Delta = line.Qty,
                            OrderLineId = line.OrderLineId
                        },
                        ct
                    );
                }
            }
        }
        
        order.ApplyCancel();
        
        await _db.SaveChangesAsync(ct);

        var lines_t = await _db.OrderLines
            .AsNoTracking()
            .Where(l => l.BusinessId == businessId && l.OrderId == orderId)
            .ToListAsync(ct);

        return order.ToDetailResponse(lines_t);
    }

    
    public async Task<OrderDetailResponse> RefundOrderAsync(
        int businessId,
        int orderId,
        int callerEmployeeId,
        CancelOrderRequest request,
        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);
        var order = await GetOrderEntityAsync(businessId, orderId, ct);
        EnsureCallerCanSeeOrder(caller, order);

        if (!string.Equals(order.Status, "closed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Order can only be refunded if it was closed");
        
        
        //EnsureOpen(order);

        
        
        var lines = await _db.OrderLines
            .Where(l => l.OrderId == orderId && l.BusinessId == businessId)
            .ToListAsync(ct);

        foreach (var line in lines)
        {
            var item = await _db.CatalogItems
                           .AsNoTracking()
                           .FirstOrDefaultAsync(
                               ci => ci.BusinessId == businessId && ci.CatalogItemId == line.CatalogItemId, ct)
                       ?? throw new InvalidOperationException("Catalog item not found in this business.");
            
            // Only return stock for products, not services
            if (string.Equals(item.Type, "Product", StringComparison.OrdinalIgnoreCase))
            {
                var stockItem = await _db.StockItems
                    .FirstOrDefaultAsync(s => s.CatalogItemId == line.CatalogItemId, ct);
                
                if (stockItem != null)
                {
                    await _stockMovement.CreateAsync(
                        businessId,
                        stockItem.StockItemId,
                        callerEmployeeId,
                        new CreateStockMovementRequest
                        {
                            Type = "Adjust",
                            Delta = line.Qty,
                            OrderLineId = line.OrderLineId
                        },
                        ct
                    );
                }
            }
        }
        
        order.ApplyRefund();
        
        await _db.SaveChangesAsync(ct);

        var lines_t = await _db.OrderLines
            .AsNoTracking()
            .Where(l => l.BusinessId == businessId && l.OrderId == orderId)
            .ToListAsync(ct);

        return order.ToDetailResponse(lines_t);
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
        int? discountId = null;
        if (discount != null)
        {
            discountId = discount.DiscountId;
            snapshot = _discounts.MakeLineDiscountSnapshot(discount, request.CatalogItemId, null);
        }

        // Only check stock for products, not services
        StockItem? stockItem = null;
        if (string.Equals(item.Type, "Product", StringComparison.OrdinalIgnoreCase))
        {
            stockItem = await _db.StockItems
                .FirstOrDefaultAsync(s => s.CatalogItemId == item.CatalogItemId, ct)
                ?? throw new InvalidOperationException("stock_item_not_found");

            if (stockItem.QtyOnHand < request.Qty)
                throw new InvalidOperationException("not_enough_stock");
        }

        var line = request.ToNewLineEntity(
            businessId: businessId,
            orderId: orderId,
            performedByEmployeeId: callerEmployeeId,
            itemNameSnapshot: item.Name,
            unitPriceSnapshot: item.BasePrice,
            catalogTypeSnapshot: item.Type,
            taxClassSnapshot: item.TaxClass,
            taxRateSnapshotPct: taxRate,
            discountId:discountId,
            unitDiscountSnapshot: snapshot,
            nowUtc: DateTime.UtcNow
        );

        
       
        _db.OrderLines.Add(line);
        await _db.SaveChangesAsync(ct);
        
        // Only create stock movement for products (not services)
        if (string.Equals(item.Type, "Product", StringComparison.OrdinalIgnoreCase) && stockItem != null)
        {
            await _stockMovement.CreateAsync(
                businessId: businessId,
                stockItemId: stockItem.StockItemId,
                callerEmployeeId: callerEmployeeId,
                new CreateStockMovementRequest
                {
                    Type = "sale",
                    Delta = -request.Qty,
                    OrderLineId = line.OrderLineId
                },
                ct
            );
        }

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
        //---------------------------------------------------------------------------------------------------------------------
        //ensure open tik staffui, manager ir owner galetu pakeisti update line ir t.t mental not reikia padaryti !!!!!!!!!
        //---------------------------------------------------------------------------------------------------------------------
        EnsureOpen(order);
        
        



        var line = await _db.OrderLines
                       .FirstOrDefaultAsync(
                           l => l.BusinessId == businessId && l.OrderId == orderId && l.OrderLineId == orderLineId, ct)
                   ?? throw new InvalidOperationException("Order line not found.");
        decimal oldQty = line.Qty;
        decimal newQty = request.Qty;
        decimal diff = newQty - oldQty;

        
         string? refreshedDiscountSnapshot = null;
                if (request.DiscountId.HasValue)
                {
                    var discount = await _discounts.EnsureLineDiscountEligibleAsync(businessId, (int)request.DiscountId,
                        line.CatalogItemId, null, ct);
                    refreshedDiscountSnapshot = _discounts.MakeLineDiscountSnapshot(discount, line.CatalogItemId);
                }
        
        
        if (diff != 0)
        {
            var item = await _db.CatalogItems
                           .AsNoTracking()
                           .FirstOrDefaultAsync(
                               ci => ci.BusinessId == businessId && ci.CatalogItemId == line.CatalogItemId, ct)
                       ?? throw new InvalidOperationException("Catalog item not found in this business.");

            // Only check/update stock for products, not services
            if (string.Equals(item.Type, "Product", StringComparison.OrdinalIgnoreCase))
            {
                var stockItem = await _db.StockItems
                    .FirstOrDefaultAsync(s => s.CatalogItemId == line.CatalogItemId, ct)
                    ?? throw new InvalidOperationException("stock_item_not_found");
                
                if (diff > 0)
                {
                    if (stockItem.QtyOnHand < diff)
                        throw new InvalidOperationException("not_enough_stock");

                    await _stockMovement.CreateAsync(
                        businessId,
                        stockItem.StockItemId,
                        callerEmployeeId,
                        new CreateStockMovementRequest
                        {
                            Type = "Sale",
                            Delta = -diff,
                            OrderLineId = line.OrderLineId
                        },
                        ct
                    );
                }
                else
                {
                    // Return stock
                    await _stockMovement.CreateAsync(
                        businessId,
                        stockItem.StockItemId,
                        callerEmployeeId,
                        new CreateStockMovementRequest
                        {
                            Type = "Adjust",
                            Delta = Math.Abs(diff),
                            OrderLineId = line.OrderLineId
                        },
                        ct
                    );
                }
            }
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



        var stockItem = await _db.StockItems
            .FirstOrDefaultAsync(s => s.CatalogItemId == line.CatalogItemId, ct);

        var item = await _db.CatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ci => ci.BusinessId == businessId && ci.CatalogItemId == line.CatalogItemId, ct)
                   ?? throw new InvalidOperationException("Catalog item not found in this business.");
            
            
        if (stockItem != null && string.Equals(item.Type, "product", StringComparison.OrdinalIgnoreCase))
        {
            
                await _stockMovement.CreateAsync(
                    businessId,
                    stockItem.StockItemId,
                    callerEmployeeId,
                    new CreateStockMovementRequest
                    {
                        Type = "Adjust",
                        Delta = line.Qty,
                        OrderLineId = line.OrderLineId
                    },
                    ct
                );
            
                
        }
        _db.OrderLines.Remove(line);
        await _db.SaveChangesAsync(ct);
    }


    public async Task<OrderDetailResponse> ReopenOrderAsync(
        int businessId,
        int orderId,
        int callerEmployeeId,
        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);

        if (!IsManagerOrOwner(caller))
            throw new InvalidOperationException("Forbidden: only managers/owners can reopen orders.");

        var order = await GetOrderEntityAsync(businessId, orderId, ct);

        if (order.Status == "Open")
            throw new InvalidOperationException("Order is already open.");
        
        order.Status = "Open";
        order.ClosedAt = null;

        await _db.SaveChangesAsync(ct);

        var lines = await _db.OrderLines
            .AsNoTracking()
            .Where(l => l.BusinessId == businessId && l.OrderId == orderId)
            .ToListAsync(ct);

        return order.ToDetailResponse(lines);
    }

    
    
    public async Task MoveLinesAsync(
    int businessId,
    int fromOrderId,
    int callerEmployeeId,
    MoveOrderLinesRequest request,
    CancellationToken ct = default)
{
    if (request == null) throw new InvalidOperationException("bad request");
    if (request.TargetOrderId <= 0) throw new InvalidOperationException("target order not found");
    if (request.Lines == null || request.Lines.Count == 0) throw new InvalidOperationException("no lines to move");

    
    foreach (var r in request.Lines)
    {
        if (r.OrderLineId <= 0) throw new InvalidOperationException("bad order line id");
        if (r.Qty <= 0) throw new InvalidOperationException("qty must be positive");
    }

    if (request.TargetOrderId == fromOrderId)
        throw new InvalidOperationException("target order same as source");

    var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);

    var fromOrder = await GetOrderEntityAsync(businessId, fromOrderId, ct);
    EnsureCallerCanSeeOrder(caller, fromOrder);
    EnsureOpen(fromOrder);

    var targetOrder = await GetOrderEntityAsync(businessId, request.TargetOrderId, ct);
    EnsureCallerCanSeeOrder(caller, targetOrder);
    EnsureOpen(targetOrder);

    
    var requestedIds = request.Lines.Select(x => x.OrderLineId).ToList();
    var distinctIds = requestedIds.Distinct().ToList();
    if (distinctIds.Count != requestedIds.Count)
        throw new InvalidOperationException("duplicate order line id in request");

   
    var sourceLines = await _db.OrderLines
        .Where(l => l.BusinessId == businessId
                    && l.OrderId == fromOrderId
                    && distinctIds.Contains(l.OrderLineId))
        .ToListAsync(ct);

    if (sourceLines.Count != distinctIds.Count)
        throw new InvalidOperationException("one or more lines not found in source order");

   
    await using var tx = await _db.Database.BeginTransactionAsync(ct);

    try
    {
       
        var byId = sourceLines.ToDictionary(x => x.OrderLineId, x => x);

        foreach (var move in request.Lines)
        {
            var line = byId[move.OrderLineId];

            
            if (line.Qty <= 0)
                throw new InvalidOperationException("line qty invalid");

            if (move.Qty > line.Qty)
                throw new InvalidOperationException("qty exceeds available");

            
            if (move.Qty == line.Qty)
            {
                line.OrderId = targetOrder.OrderId;
                continue;
            }

         
            line.Qty -= move.Qty;

            if (line.Qty <= 0)
                throw new InvalidOperationException("resulting qty invalid");

            var clone = new OrderLine
            {
                BusinessId = line.BusinessId,
                OrderId = targetOrder.OrderId,
                CatalogItemId = line.CatalogItemId,
                DiscountId = line.DiscountId,

                Qty = move.Qty,

              
                ItemNameSnapshot = line.ItemNameSnapshot,
                UnitPriceSnapshot = line.UnitPriceSnapshot,
                UnitDiscountSnapshot = line.UnitDiscountSnapshot,
                CatalogTypeSnapshot = line.CatalogTypeSnapshot,
                TaxClassSnapshot = line.TaxClassSnapshot,
                TaxRateSnapshotPct = line.TaxRateSnapshotPct,
                
                PerformedAt = line.PerformedAt,
                PerformedByEmployeeId = line.PerformedByEmployeeId
            };

            _db.OrderLines.Add(clone);
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    catch
    {
        await tx.RollbackAsync(ct);
        throw;
    }
}
    
    
    
    



}
    