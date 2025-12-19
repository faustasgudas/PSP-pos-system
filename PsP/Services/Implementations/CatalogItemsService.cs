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

   
    private Task<CatalogItem?> GetItemAsync(int businessId, int catalogItemId, bool tracking = true)
    {
        var q = _db.CatalogItems
            .Where(c => c.BusinessId == businessId && c.CatalogItemId == catalogItemId);

        if (!tracking)
            q = q.AsNoTracking();

        return q.FirstOrDefaultAsync();
    }

   
    public async Task<IEnumerable<CatalogItemSummaryResponse>> ListAllAsync(
        int businessId,
        int callerEmployeeId,  
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

    
    public async Task<CatalogItemDetailResponse?> GetByIdAsync(
        int businessId,
        int catalogItemId,
        int callerEmployeeId) 
    {
        var item = await GetItemAsync(businessId, catalogItemId, tracking: false);
        return item?.ToDetailResponse();
    }

    public async Task<CatalogItemDetailResponse> CreateAsync(
        int businessId,
        int callerEmployeeId,   
        CreateCatalogItemRequest body)
    {
        
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

        
        var entity = body.ToNewEntity(businessId);

        _db.CatalogItems.Add(entity);
        await _db.SaveChangesAsync();

        return entity.ToDetailResponse();
    }

  
    public async Task<CatalogItemDetailResponse?> UpdateAsync(
        int businessId,
        int catalogItemId,
        int callerEmployeeId,   
        UpdateCatalogItemRequest body)
    {
        var entity = await GetItemAsync(businessId, catalogItemId, tracking: true);
        if (entity is null)
            return null;

     
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

   
    public async Task<bool> ArchiveAsync(
        int businessId,
        int catalogItemId,
        int callerEmployeeId)   
    {
        var entity = await GetItemAsync(businessId, catalogItemId, tracking: true);
        if (entity is null)
            return false;

        if (string.Equals(entity.Status, "Archived", StringComparison.OrdinalIgnoreCase))
            return true; 

        entity.Status = "Archived";
        await _db.SaveChangesAsync();

        return true;
    }
}
