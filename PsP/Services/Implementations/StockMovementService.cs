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
            ?? throw new InvalidOperationException("caller_not_found_in_business");

        if (!string.Equals(caller.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("caller_inactive");

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
               ?? throw new InvalidOperationException("stock_item_not_found");
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
        if (t.Equals("receive", StringComparison.OrdinalIgnoreCase))      return "Receive";
        if (t.Equals("sale", StringComparison.OrdinalIgnoreCase))         return "Sale";
        if (t.Equals("refundreturn", StringComparison.OrdinalIgnoreCase)) return "RefundReturn";
        if (t.Equals("waste", StringComparison.OrdinalIgnoreCase))        return "Waste";
        if (t.Equals("adjust", StringComparison.OrdinalIgnoreCase))       return "Adjust";
        return t;
    }

    private static void ValidateType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new InvalidOperationException("invalid_type");

        var normalized = NormalizeType(type);
        if (!ValidTypes.Contains(normalized))
            throw new InvalidOperationException("invalid_type");
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
            ValidateType(type);
            var normalized = NormalizeType(type);
            q = q.Where(m => m.Type == normalized);
        }

        if (dateFrom.HasValue) q = q.Where(m => m.At >= dateFrom.Value);
        if (dateTo.HasValue)   q = q.Where(m => m.At <= dateTo.Value);

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
            ?? throw new InvalidOperationException("stock_movement_not_found");

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
            throw new InvalidOperationException("delta_cannot_be_zero");

        _ = await GetCallerAsync(businessId, callerEmployeeId, ct);

        if (request.OrderLineId.HasValue)
        {
            var line = await GetOrderLineAsync(businessId, request.OrderLineId.Value, ct);
            if (line == null)
                throw new InvalidOperationException("order_line_not_found");
        }

        var movement = request.ToNewEntity(stockItemId, DateTime.UtcNow);

        if (string.Equals(movement.Type, "Receive", StringComparison.OrdinalIgnoreCase)
            && movement.Delta > 0
            && !movement.UnitCostSnapshot.HasValue)
        {
            throw new InvalidOperationException("unit_cost_required_for_receive");
        }


        
        
        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            if (attempt > 0)
                _db.ChangeTracker.Clear();

            var stockItem = await GetStockItemAsync(businessId, stockItemId, tracking: true, ct);

            if (request.OrderLineId.HasValue)
            {
                var line = await GetOrderLineAsync(businessId, request.OrderLineId.Value, ct);
                if (line!.CatalogItemId != stockItem.CatalogItemId)
                    throw new InvalidOperationException("order_line_mismatch_stock_item");
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
                    throw new InvalidOperationException("concurrency_conflict");
            }
        }

        throw new InvalidOperationException("concurrency_conflict");
    }
}
