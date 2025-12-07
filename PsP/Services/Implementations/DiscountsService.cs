using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Catalog;
using PsP.Contracts.Discounts;
using PsP.Contracts.Snapshots;
using PsP.Data;
using PsP.Mappings;
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

    private async Task<Discount> GetDiscountOr404(int businessId, int discountId, CancellationToken ct)
    {
        var d = await _db.Discounts
            .Include(x => x.Eligibilities)
            .FirstOrDefaultAsync(x => x.DiscountId == discountId && x.BusinessId == businessId, ct);
        if (d is null) throw new InvalidOperationException("Discount not found");
        return d;
    }

    // ---------- Discounts ----------
    public async Task<IEnumerable<DiscountSummaryResponse>> ListDiscountsAsync(
        int businessId, int callerId, CancellationToken ct = default)
    {
        // Any employee in the business can list
        _ = await GetCallerAsync(businessId, callerId, ct);

        var q = await _db.Discounts
            .AsNoTracking()
            .Where(d => d.BusinessId == businessId)
            .OrderByDescending(d => d.StartsAt)
            .ThenByDescending(d => d.DiscountId)
            .ToListAsync(ct);

        return q.ToSummaryResponses(); // uses your mapper
    }

    public async Task<DiscountDetailResponse> GetDiscountAsync(
        int businessId, int callerId, int discountId, CancellationToken ct = default)
    {
        _ = await GetCallerAsync(businessId, callerId, ct);

        var d = await _db.Discounts
            .AsNoTracking()
            .Include(x => x.Eligibilities)
            .FirstOrDefaultAsync(x => x.DiscountId == discountId && x.BusinessId == businessId, ct);

        if (d is null) throw new InvalidOperationException("Discount not found");
        return d.ToDetailResponse(); // mapper
    }

    public async Task<DiscountDetailResponse> CreateDiscountAsync(
        int businessId, int callerId, CreateDiscountRequest body, CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerId, ct);
        if (!IsManagerOrOwner(caller))
            throw new InvalidOperationException("Forbidden: only Manager/Owner can create discounts");

        // Validate uniqueness of code within business
        var codeNorm = (body.Code ?? "").Trim().ToUpperInvariant();
        var exists = await _db.Discounts
            .AsNoTracking()
            .AnyAsync(d => d.BusinessId == businessId && d.Code == codeNorm, ct);
        if (exists) throw new InvalidOperationException("Discount code already exists for this business");

        var entity = body.ToNewEntity(businessId); // mapper validates fields
        _db.Discounts.Add(entity);
        await _db.SaveChangesAsync(ct);

        // reload with eligibilities (empty on create)
        var created = await _db.Discounts
            .AsNoTracking()
            .Include(x => x.Eligibilities)
            .FirstAsync(x => x.DiscountId == entity.DiscountId, ct);

        return created.ToDetailResponse();
    }

    public async Task<DiscountDetailResponse> UpdateDiscountAsync(
        int businessId, int callerId, int discountId, UpdateDiscountRequest body, CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerId, ct);
        if (!IsManagerOrOwner(caller))
            throw new InvalidOperationException("Forbidden: only Manager/Owner can update discounts");

        var d = await GetDiscountOr404(businessId, discountId, ct);

        // If code changes, enforce uniqueness
        if (!string.IsNullOrWhiteSpace(body.Code))
        {
            var newCode = body.Code.Trim().ToUpperInvariant();
            var taken = await _db.Discounts
                .AsNoTracking()
                .AnyAsync(x => x.BusinessId == businessId && x.Code == newCode && x.DiscountId != discountId, ct);
            if (taken) throw new InvalidOperationException("Discount code already exists for this business");
        }

        body.ApplyUpdate(d); // safe partial update (enforces window/value) via mapper

        await _db.SaveChangesAsync(ct);

        // Return fresh detail
        var updated = await _db.Discounts
            .AsNoTracking()
            .Include(x => x.Eligibilities)
            .FirstAsync(x => x.DiscountId == discountId, ct);
        return updated.ToDetailResponse();
    }

    public async Task DeleteDiscountAsync(
        int businessId, int callerId, int discountId, CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerId, ct);
        if (!IsManagerOrOwner(caller))
            throw new InvalidOperationException("Forbidden: only Manager/Owner can delete discounts");

        var d = await _db.Discounts
            .Include(x => x.Eligibilities)
            .FirstOrDefaultAsync(x => x.DiscountId == discountId && x.BusinessId == businessId, ct);
        if (d is null) return; // idempotent

        _db.Discounts.Remove(d);
        await _db.SaveChangesAsync(ct);
    }

    // ---------- Eligibilities ----------
    public async Task<IEnumerable<DiscountEligibilityResponse>> ListEligibilitiesAsync(
        int businessId, int callerId, int discountId, CancellationToken ct = default)
    {
        _ = await GetCallerAsync(businessId, callerId, ct);
        // Ensure discount belongs to business
        var exists = await _db.Discounts
            .AsNoTracking()
            .AnyAsync(x => x.DiscountId == discountId && x.BusinessId == businessId, ct);
        if (!exists) throw new InvalidOperationException("Discount not found");

        var q = await _db.DiscountEligibilities
            .AsNoTracking()
            .Where(e => e.DiscountId == discountId)
            .OrderBy(e => e.CatalogItemId)
            .ToListAsync(ct);

        return q.ToResponses();
    }

    public async Task<IEnumerable<CatalogItemSummaryResponse>> ListEligibleItemsAsync(
        int businessId, int callerId, int discountId, CancellationToken ct = default)
    {
        _ = await GetCallerAsync(businessId, callerId, ct);
        var ok = await _db.Discounts.AsNoTracking()
            .AnyAsync(d => d.DiscountId == discountId && d.BusinessId == businessId, ct);
        if (!ok) throw new InvalidOperationException("Discount not found");

        var items = await _db.DiscountEligibilities
            .AsNoTracking()
            .Where(e => e.DiscountId == discountId)
            .Join(_db.CatalogItems,
                e => e.CatalogItemId,
                i => i.CatalogItemId,
                (e, i) => i)
            .OrderBy(i => i.Name)
            .ToListAsync(ct);

        return items.Select(i => i.ToSummaryResponse());
    }
    
    public async Task<DiscountEligibilityResponse> AddEligibilityAsync(
        int businessId, int callerId, int discountId, CreateDiscountEligibilityRequest body, CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerId, ct);
        if (!IsManagerOrOwner(caller))
            throw new InvalidOperationException("Forbidden: only Manager/Owner can add eligibilities");

        var d = await GetDiscountOr404(businessId, discountId, ct);

        // Ensure the catalog item is in the same business
        var itemExists = await _db.CatalogItems
            .AsNoTracking()
            .AnyAsync(ci => ci.CatalogItemId == body.CatalogItemId && ci.BusinessId == businessId, ct);
        if (!itemExists) throw new InvalidOperationException("Catalog item not found for this business");

        // Prevent duplicates gracefully
        var already = await _db.DiscountEligibilities
            .AsNoTracking()
            .AnyAsync(e => e.DiscountId == discountId && e.CatalogItemId == body.CatalogItemId, ct);
        if (already) throw new InvalidOperationException("Eligibility already exists for this catalog item");

        var entity = body.ToNewEntity(discountId);
        _db.DiscountEligibilities.Add(entity);

        // Save & return response
        await _db.SaveChangesAsync(ct);
        return entity.ToResponse();
    }

    public async Task RemoveEligibilityAsync(
        int businessId, int callerId, int discountId, int catalogItemId, CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerId, ct);
        if (!IsManagerOrOwner(caller))
            throw new InvalidOperationException("Forbidden: only Manager/Owner can remove eligibilities");

        // Ensure discount belongs to business
        var ok = await _db.Discounts
            .AsNoTracking()
            .AnyAsync(x => x.DiscountId == discountId && x.BusinessId == businessId, ct);
        if (!ok) throw new InvalidOperationException("Discount not found");

        var row = await _db.DiscountEligibilities
            .FirstOrDefaultAsync(e => e.DiscountId == discountId && e.CatalogItemId == catalogItemId, ct);

        if (row is null) return; // idempotent

        _db.DiscountEligibilities.Remove(row);
        await _db.SaveChangesAsync(ct);
    }
    
    

    
    
}