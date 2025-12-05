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
        // Validacija jau yra ToNewEntity viduje, bet gali likti ir čia jei nori
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

        entity.Status = "Archived"; // NormalizeCatalogStatus suveiks vėliau, jei dar kur nors kviestum
        await _db.SaveChangesAsync();

        return true;
    }
}
