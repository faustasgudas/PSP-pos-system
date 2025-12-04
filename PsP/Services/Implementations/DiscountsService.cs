using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PsP.Data;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations;

public class DiscountsService : IDiscountsService
{
    
    private readonly AppDbContext _db;

    public DiscountsService(AppDbContext db) => _db = db;

    public async Task<Discount?> GetNewestOrderDiscountAsync(
        int businessId,
        DateTime? nowUtc = null,
        CancellationToken ct = default)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        return await _db.Discounts
            .AsNoTracking()
            .Where(d =>
                d.BusinessId == businessId &&
                d.Status == "Active" &&
                d.Scope  == "Order" &&
                d.StartsAt <= now &&
                d.EndsAt   >= now)
            .OrderByDescending(d => d.StartsAt)
            .ThenByDescending(d => d.DiscountId)
            .FirstOrDefaultAsync(ct);
    }
    
    
    public async Task<Discount?> GetOrderDiscountAsync(
        int discountId,
        CancellationToken ct = default)
    {;
        
        return await _db.Discounts
            .AsNoTracking()
            .Where(d =>
                d.DiscountId == discountId)
            .FirstOrDefaultAsync(ct);
    }
    
    
    public async Task<Discount?> GetNewestLineDiscountForItemAsync(
        int businessId,
        int catalogItemId,
        DateTime? nowUtc = null,
        CancellationToken ct = default)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        return await _db.Discounts
            .AsNoTracking()
            .Where(d =>
                d.BusinessId == businessId &&
                d.Status == "Active" &&
                d.Scope  == "Line" &&
                d.StartsAt <= now &&
                d.EndsAt   >= now &&
                d.Eligibilities.Any(e => e.CatalogItemId == catalogItemId))
            .OrderByDescending(d => d.StartsAt)
            .ThenByDescending(d => d.DiscountId)
            .FirstOrDefaultAsync(ct);
    }
    
    public async Task<Discount> EnsureLineDiscountEligibleAsync(
        int businessId,
        int discountId,
        int catalogItemId,
        DateTime? nowUtc = null,
        CancellationToken ct = default)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        
        var itemExists = await _db.CatalogItems
            .AsNoTracking()
            .AnyAsync(ci => ci.CatalogItemId == catalogItemId &&
                            ci.BusinessId    == businessId, ct);
        if (!itemExists)
            throw new InvalidOperationException("Catalog item not found for this business.");

        
        var discount = await _db.Discounts
            .AsNoTracking()
            .Where(d =>
                d.DiscountId == discountId &&
                d.BusinessId == businessId &&
                d.Status     == "Active" &&
                d.Scope      == "Line" &&
                d.StartsAt   <= now &&
                d.EndsAt     >= now &&
                d.Eligibilities.Any(e => e.CatalogItemId == catalogItemId))
            .OrderByDescending(d => d.StartsAt)
            .ThenByDescending(d => d.DiscountId)
            .FirstOrDefaultAsync(ct);

        if (discount is null)
            throw new InvalidOperationException("Discount is not eligible for this item.");

        return discount;
    }

    public async Task<Discount> EnsureOrderDiscountEligibleAsync(
        int businessId,
        int discountId,
        DateTime? nowUtc = null,
        CancellationToken ct = default)
    {
        var now = nowUtc ?? DateTime.UtcNow;

       
        var discount = await _db.Discounts
            .AsNoTracking()
            .Where(d =>
                d.DiscountId == discountId &&
                d.BusinessId == businessId &&
                d.Status     == "Active" &&
                d.Scope      == "Order" &&
                d.StartsAt   <= now &&
                d.EndsAt     >= now)
            .OrderByDescending(d => d.StartsAt)
            .ThenByDescending(d => d.DiscountId)
            .FirstOrDefaultAsync(ct);

        if (discount is null)
            throw new InvalidOperationException("Order-level discount is not eligible.");

        return discount;
    }
    
    
    public string MakeOrderDiscountSnapshot(Discount d, DateTime? capturedAtUtc = null)
    {
        var payload = new
        {
            discountId = d.DiscountId,
            code = d.Code,
            type = d.Type,        // "Percent" | "Amount"
            value = d.Value,      // decimal (money or percent depending on Type)
            scope = d.Scope,      // "Order"
            validFrom = d.StartsAt,
            validTo   = d.EndsAt,
            capturedAtUtc = capturedAtUtc ?? DateTime.UtcNow
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public string MakeLineDiscountSnapshot(Discount d, int catalogItemId, DateTime? capturedAtUtc = null)
    {
        var payload = new
        {
            discountId = d.DiscountId,
            code = d.Code,
            type = d.Type,
            value = d.Value,
            scope = d.Scope,      // "Line"
            catalogItemId,
            validFrom = d.StartsAt,
            validTo   = d.EndsAt,
            capturedAtUtc = capturedAtUtc ?? DateTime.UtcNow
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };
    
    

    
    
}