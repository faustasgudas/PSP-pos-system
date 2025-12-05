using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Snapshots;
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
    {
        
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
                (d.EndsAt == null || d.EndsAt >= now)
                 &&
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
                (d.EndsAt == null || d.EndsAt >= now))
            .OrderByDescending(d => d.StartsAt)
            .ThenByDescending(d => d.DiscountId)
            .FirstOrDefaultAsync(ct);

        if (discount is null)
            throw new InvalidOperationException("Order-level discount is not eligible.");

        return discount;
    }
    
    


static readonly JsonSerializerOptions JsonOptions = new()
{
    WriteIndented = false,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
};

// --- Writers ---
public string MakeOrderDiscountSnapshot(Discount d, DateTime? capturedAtUtc = null)
{
   

    var snap = new DiscountSnapshot
    {
        Version = 1,
        DiscountId = d.DiscountId,
        Code = d.Code,
        Type = d.Type,
        Scope = d.Scope,           // "Order"
        Value = d.Value,
        ValidFrom = d.StartsAt,
        ValidTo   = d.EndsAt,
        CapturedAtUtc = capturedAtUtc ?? DateTime.UtcNow
    };
    return JsonSerializer.Serialize(snap, JsonOptions);
}

public string MakeLineDiscountSnapshot(Discount d, int catalogItemId, DateTime? capturedAtUtc = null)
{
    var snap = new DiscountSnapshot
    {
        Version = 1,
        DiscountId = d.DiscountId,
        Code = d.Code,
        Type = d.Type,
        Scope = d.Scope,          
        Value = d.Value,
        CatalogItemId = catalogItemId,
        ValidFrom = d.StartsAt,
        ValidTo   = d.EndsAt,
        CapturedAtUtc = capturedAtUtc ?? DateTime.UtcNow
    };
    return JsonSerializer.Serialize(snap, JsonOptions);
}


public DiscountSnapshot? TryParseDiscountSnapshot(string? json)
{
    if (string.IsNullOrWhiteSpace(json)) return null;
    try { return JsonSerializer.Deserialize<DiscountSnapshot>(json, JsonOptions); }
    catch { return null; } 
}

    
    

    
    
}