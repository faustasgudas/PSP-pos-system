using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Catalog;
using PsP.Data;
using PsP.Mappings;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations;

public class CatalogItemsService : ICatalogItemsService
{
    private readonly AppDbContext _db;

    public CatalogItemsService(AppDbContext db)
    {
        _db = db;
    }

    // Helper: single item
    private Task<CatalogItem?> GetItemAsync(int businessId, int catalogItemId, bool tracking = true)
    {
        var q = _db.CatalogItems
            .Where(c => c.BusinessId == businessId && c.CatalogItemId == catalogItemId);

        if (!tracking)
            q = q.AsNoTracking();

        return q.FirstOrDefaultAsync();
    }

    // LIST
    public async Task<IEnumerable<CatalogItemSummaryResponse>> ListAllAsync(
        int businessId,
        int callerEmployeeId,   // JWT, ignore
        string? type,
        string? status,
        string? code)
    {
        var q = _db.CatalogItems
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId);

        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(c => c.Type == type);

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(code))
            q = q.Where(c => c.Code == code);

        var items = await q
            .OrderBy(c => c.Name)
            .ToListAsync();

        return items.Select(c => c.ToSummaryResponse());
    }

    // GET BY ID
    public async Task<CatalogItemDetailResponse?> GetByIdAsync(
        int businessId,
        int catalogItemId,
        int callerEmployeeId) // JWT, ignore
    {
        var item = await GetItemAsync(businessId, catalogItemId, tracking: false);
        return item?.ToDetailResponse();
    }

    // CREATE
    public async Task<CatalogItemDetailResponse> CreateAsync(
        int businessId,
        int callerEmployeeId,   // JWT, ignore
        CreateCatalogItemRequest body)
    {
        // 1) patikrinam ar Code jau nenaudojamas tame pačiame business
        if (!string.IsNullOrWhiteSpace(body.Code))
        {
            var exists = await _db.CatalogItems
                .AsNoTracking()
                .AnyAsync(c =>
                    c.BusinessId == businessId &&
                    c.Code == body.Code);

            if (exists)
                throw new InvalidOperationException("catalog_item_code_already_exists");
        }

        // 2) sukuriam entity
        var entity = body.ToNewEntity(businessId);

        _db.CatalogItems.Add(entity);
        await _db.SaveChangesAsync();

        return entity.ToDetailResponse();
    }

    // UPDATE
    public async Task<CatalogItemDetailResponse?> UpdateAsync(
        int businessId,
        int catalogItemId,
        int callerEmployeeId,   // JWT, ignore
        UpdateCatalogItemRequest body)
    {
        var entity = await GetItemAsync(businessId, catalogItemId, tracking: true);
        if (entity is null)
            return null;

        // Jei request'e keičiam Code – reikia vėl patikrint unikalumą
        if (!string.IsNullOrWhiteSpace(body.Code) &&
            !string.Equals(body.Code, entity.Code, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _db.CatalogItems
                .AsNoTracking()
                .AnyAsync(c =>
                    c.BusinessId == businessId &&
                    c.Code == body.Code &&
                    c.CatalogItemId != catalogItemId);

            if (exists)
                throw new InvalidOperationException("catalog_item_code_already_exists");
        }

        body.ApplyUpdate(entity);
        await _db.SaveChangesAsync();

        return entity.ToDetailResponse();
    }

    // ARCHIVE
    public async Task<bool> ArchiveAsync(
        int businessId,
        int catalogItemId,
        int callerEmployeeId)   // JWT, ignore
    {
        var entity = await GetItemAsync(businessId, catalogItemId, tracking: true);
        if (entity is null)
            return false;

        if (string.Equals(entity.Status, "Archived", StringComparison.OrdinalIgnoreCase))
            return true; // idempotent

        entity.Status = "Archived";
        await _db.SaveChangesAsync();

        return true;
    }
}
