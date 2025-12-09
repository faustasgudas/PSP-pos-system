using Microsoft.EntityFrameworkCore;
using PsP.Contracts.StockItems;
using PsP.Data;
using PsP.Mappings;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations;

public class StockItemService : IStockItemService
{
    private readonly AppDbContext _db;

    public StockItemService(AppDbContext db)
    {
        _db = db;
    }

    private async Task<Employee> EnsureCallerIsManagerOrOwnerAsync(
        int businessId,
        int callerEmployeeId,
        CancellationToken ct)
    {
        var caller = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == callerEmployeeId, ct)
            ?? throw new InvalidOperationException("caller_not_found_or_wrong_business");

        if (caller.BusinessId != businessId)
            throw new InvalidOperationException("caller_not_found_or_wrong_business");

        if (!caller.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("caller_inactive");

        var isOwner = caller.Role.Equals("Owner", StringComparison.OrdinalIgnoreCase);
        var isManager = caller.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase);

        if (!isOwner && !isManager)
            throw new InvalidOperationException("forbidden");

        return caller;
    }

    private async Task<CatalogItem> GetCatalogItemForBusinessAsync(
        int businessId,
        int catalogItemId,
        CancellationToken ct)
    {
        return await _db.CatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(ci => ci.CatalogItemId == catalogItemId && ci.BusinessId == businessId, ct)
            ?? throw new InvalidOperationException("catalog_item_not_found");
    }

    public async Task<IEnumerable<StockItemSummaryResponse>> ListAsync(
        int businessId,
        int callerEmployeeId,
        int? catalogItemId,
        CancellationToken ct = default)
    {
        _ = await EnsureCallerIsManagerOrOwnerAsync(businessId, callerEmployeeId, ct);

        var q = _db.StockItems
            .AsNoTracking()
            .Include(s => s.CatalogItem)
            .Where(s => s.CatalogItem!.BusinessId == businessId);

        if (catalogItemId.HasValue)
            q = q.Where(s => s.CatalogItemId == catalogItemId.Value);

        var list = await q
            .OrderBy(s => s.StockItemId)
            .ToListAsync(ct);

        return list.Select(s => s.ToSummaryResponse());
    }

    public async Task<StockItemDetailResponse?> GetOneAsync(
        int businessId,
        int stockItemId,
        int callerEmployeeId,
        CancellationToken ct = default)
    {
        _ = await EnsureCallerIsManagerOrOwnerAsync(businessId, callerEmployeeId, ct);

        var stockItem = await _db.StockItems
            .AsNoTracking()
            .Include(s => s.CatalogItem)
            .FirstOrDefaultAsync(
                s => s.StockItemId == stockItemId &&
                     s.CatalogItem!.BusinessId == businessId,
                ct);

        return stockItem?.ToDetailResponse();
    }

    public async Task<StockItemDetailResponse> CreateAsync(
        int businessId,
        int callerEmployeeId,
        CreateStockItemRequest request,
        CancellationToken ct = default)
    {
        _ = await EnsureCallerIsManagerOrOwnerAsync(businessId, callerEmployeeId, ct);

        // 1) CatalogItem turi būti tame business
        var catalogItem = await GetCatalogItemForBusinessAsync(businessId, request.CatalogItemId, ct);

        // 2) 1:1 – tik vienas StockItem per CatalogItem
        var exists = await _db.StockItems
            .AsNoTracking()
            .AnyAsync(s => s.CatalogItemId == catalogItem.CatalogItemId, ct);

        if (exists)
            throw new InvalidOperationException("stock_item_already_exists");

        // 3) sukurti entity iš request
        var entity = request.ToNewEntity();

        _db.StockItems.Add(entity);
        await _db.SaveChangesAsync(ct);

        return entity.ToDetailResponse();
    }

    public async Task<StockItemDetailResponse?> UpdateAsync(
        int businessId,
        int stockItemId,
        int callerEmployeeId,
        UpdateStockItemRequest request,
        CancellationToken ct = default)
    {
        _ = await EnsureCallerIsManagerOrOwnerAsync(businessId, callerEmployeeId, ct);

        var stockItem = await _db.StockItems
            .Include(s => s.CatalogItem)
            .FirstOrDefaultAsync(
                s => s.StockItemId == stockItemId &&
                     s.CatalogItem!.BusinessId == businessId,
                ct);

        if (stockItem is null)
            return null;

        // keičiam tik Unit, o ne QtyOnHand/Cost (tie eina per StockMovements)
        request.ApplyUpdate(stockItem);

        await _db.SaveChangesAsync(ct);

        return stockItem.ToDetailResponse();
    }
}
