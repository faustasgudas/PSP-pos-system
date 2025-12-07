using Microsoft.EntityFrameworkCore;
using PsP.Contracts.StockMovements;
using PsP.Data;
using PsP.Mappings;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations;

public class StockMovementService : IStockMovementService
{
    private readonly AppDbContext _db;
    private const int MaxConcurrencyRetries = 5;

    public StockMovementService(AppDbContext db)
    {
        _db = db;
    }

    private async Task<Employee> GetCallerAsync(int businessId, int callerEmployeeId, CancellationToken ct)
    {
        var caller = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.BusinessId == businessId && e.EmployeeId == callerEmployeeId, ct)
            ?? throw new InvalidOperationException("Caller employee not found in this business.");

        if (!string.Equals(caller.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Caller is not active.");

        return caller;
    }

    private async Task<StockItem> GetStockItemAsync(int businessId, int stockItemId, bool tracking, CancellationToken ct)
    {
        var query = _db.StockItems
            .Include(s => s.CatalogItem)
            .Where(s => s.StockItemId == stockItemId && s.CatalogItem!.BusinessId == businessId);

        if (!tracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(ct)
               ?? throw new InvalidOperationException("Stock item not found.");
    }

    private async Task<OrderLine?> GetOrderLineAsync(int businessId, int orderLineId, CancellationToken ct)
    {
        return await _db.OrderLines
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.BusinessId == businessId && l.OrderLineId == orderLineId, ct);
    }

    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Receive", "Sale", "RefundReturn", "Waste", "Adjust"
    };

    private static string NormalizeType(string type)
    {
        var t = type.Trim();
        return t.Equals("receive", StringComparison.OrdinalIgnoreCase) ? "Receive" :
               t.Equals("sale", StringComparison.OrdinalIgnoreCase) ? "Sale" :
               t.Equals("refundreturn", StringComparison.OrdinalIgnoreCase) ? "RefundReturn" :
               t.Equals("waste", StringComparison.OrdinalIgnoreCase) ? "Waste" :
               t.Equals("adjust", StringComparison.OrdinalIgnoreCase) ? "Adjust" :
               t;
    }

    private static void ValidateType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new InvalidOperationException("Invalid movement type: Type is required.");

        if (!ValidTypes.Contains(type.Trim()))
            throw new InvalidOperationException($"Invalid movement type: '{type}'. Must be one of: Receive, Sale, RefundReturn, Waste, Adjust.");
    }

    public async Task<IEnumerable<StockMovementResponse>> ListAsync(
        int businessId,
        int stockItemId,
        int callerEmployeeId,
        string? type,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken ct = default)
    {
        _ = await GetCallerAsync(businessId, callerEmployeeId, ct);
        await GetStockItemAsync(businessId, stockItemId, tracking: false, ct);

        var q = _db.StockMovements
            .AsNoTracking()
            .Where(m => m.StockItemId == stockItemId);

        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!ValidTypes.Contains(type.Trim()))
                throw new InvalidOperationException($"Invalid movement type filter: '{type}'. Must be one of: Receive, Sale, RefundReturn, Waste, Adjust.");
            
            var normalized = NormalizeType(type);
            q = q.Where(m => m.Type == normalized);
        }

        if (dateFrom.HasValue) q = q.Where(m => m.At >= dateFrom.Value);
        if (dateTo.HasValue) q = q.Where(m => m.At <= dateTo.Value);

        var list = await q
            .OrderByDescending(m => m.At)
            .ThenByDescending(m => m.StockMovementId)
            .ToListAsync(ct);

        return list.Select(m => m.ToResponse());
    }

    public async Task<StockMovementResponse> GetByIdAsync(
        int businessId,
        int stockItemId,
        int movementId,
        int callerEmployeeId,
        CancellationToken ct = default)
    {
        _ = await GetCallerAsync(businessId, callerEmployeeId, ct);
        await GetStockItemAsync(businessId, stockItemId, tracking: false, ct);

        var movement = await _db.StockMovements
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.StockMovementId == movementId && m.StockItemId == stockItemId, ct)
            ?? throw new InvalidOperationException("Stock movement not found.");

        return movement.ToResponse();
    }

    public async Task<StockMovementResponse> CreateAsync(
        int businessId,
        int stockItemId,
        int callerEmployeeId,
        CreateStockMovementRequest request,
        CancellationToken ct = default)
    {
        ValidateType(request.Type);
        
        if (request.Delta == 0)
            throw new InvalidOperationException("Delta cannot be zero.");
        
        _ = await GetCallerAsync(businessId, callerEmployeeId, ct);

        if (request.OrderLineId.HasValue)
        {
            var line = await GetOrderLineAsync(businessId, request.OrderLineId.Value, ct);
            if (line == null)
                throw new InvalidOperationException("Order line not found.");
        }

        var movement = request.ToNewEntity(stockItemId, DateTime.UtcNow);

        if (string.Equals(movement.Type, "Receive", StringComparison.OrdinalIgnoreCase)
            && movement.Delta > 0
            && !movement.UnitCostSnapshot.HasValue)
        {
            throw new InvalidOperationException("UnitCostSnapshot is required for Receive movements.");
        }

        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            if (attempt > 0)
            {
                _db.ChangeTracker.Clear();
            }

            var stockItem = await GetStockItemAsync(businessId, stockItemId, tracking: true, ct);
            
            if (request.OrderLineId.HasValue)
            {
                var line = await GetOrderLineAsync(businessId, request.OrderLineId.Value, ct);
                if (line!.CatalogItemId != stockItem.CatalogItemId)
                    throw new InvalidOperationException("Order line does not match this stock item.");
            }

            var previousQty = stockItem.QtyOnHand;
            var newQty = previousQty + movement.Delta;

            if (string.Equals(movement.Type, "Receive", StringComparison.OrdinalIgnoreCase)
                && movement.Delta > 0
                && movement.UnitCostSnapshot.HasValue)
            {
                var weightedCost = (previousQty * stockItem.AverageUnitCost) +
                                   (movement.Delta * movement.UnitCostSnapshot.Value);
                var avg = newQty <= 0
                    ? stockItem.AverageUnitCost
                    : Math.Round(weightedCost / newQty, 4, MidpointRounding.AwayFromZero);
                stockItem.AverageUnitCost = avg;
            }

            stockItem.QtyOnHand = newQty;

            _db.StockMovements.Add(movement);

            try
            {
                await _db.SaveChangesAsync(ct);
                return movement.ToResponse();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (attempt == MaxConcurrencyRetries - 1)
                    throw new InvalidOperationException("Concurrency conflict on stock item. Please retry.");
            }
        }

        throw new InvalidOperationException("Concurrency conflict on stock item. Please retry.");
    }
}

