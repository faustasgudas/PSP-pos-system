using PsP.Contracts.Discounts;
using PsP.Models;

namespace PsP.Mappings;

public static class DiscountMappings
{
      public static DiscountSummaryResponse ToSummaryResponse(this Discount d) => new()
    {
        DiscountId = d.DiscountId,
        BusinessId = d.BusinessId,
        Code       = d.Code,
        Type       = d.Type,     
        Scope      = d.Scope,    
        Value      = d.Value,
        StartsAt   = d.StartsAt,
        EndsAt     = d.EndsAt,
        Status     = d.Status
    };

    public static DiscountDetailResponse ToDetailResponse(this Discount d) => new()
    {
        DiscountId   = d.DiscountId,
        BusinessId   = d.BusinessId,
        Code         = d.Code,
        Type         = d.Type,
        Scope        = d.Scope,
        Value        = d.Value,
        StartsAt     = d.StartsAt,
        EndsAt       = d.EndsAt,
        Status       = d.Status,
        
        Eligibilities = d.Eligibilities?
            .Select(e => e.ToResponse())
            .ToList() ?? new List<DiscountEligibilityResponse>()
    };

   
    public static Discount ToNewEntity(this CreateDiscountRequest req, int businessId)
    {
        if (string.IsNullOrWhiteSpace(req.Code))  throw new ArgumentException("Code is required");
        if (string.IsNullOrWhiteSpace(req.Type))  throw new ArgumentException("Type is required");
        if (string.IsNullOrWhiteSpace(req.Scope)) throw new ArgumentException("Scope is required");

        var code  = req.Code.Trim().ToUpperInvariant();
        var type  = NormalizeType(req.Type);
        var scope = NormalizeScope(req.Scope);
        var status = NormalizeStatus(req.Status ?? "Active");

        if (req.StartsAt >= req.EndsAt)
            throw new ArgumentException("StartsAt must be before EndsAt");

        if (req.Value <= 0m)
            throw new ArgumentException("Value must be positive");

        return new Discount
        {
            BusinessId = businessId,
            Code       = code,
            Type       = type,
            Scope      = scope,
            Value      = req.Value,
            StartsAt   = req.StartsAt,
            EndsAt     = req.EndsAt,
            Status     = status
        };
    }

    
    public static void ApplyUpdate(this UpdateDiscountRequest req, Discount d)
    {
        if (!string.IsNullOrWhiteSpace(req.Code))
            d.Code = req.Code.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(req.Type))
            d.Type = NormalizeType(req.Type!);

        if (!string.IsNullOrWhiteSpace(req.Scope))
            d.Scope = NormalizeScope(req.Scope!);

        if (req.Value.HasValue)
        {
            if (req.Value.Value <= 0m) throw new ArgumentException("Value must be positive");
            d.Value = req.Value.Value;
        }

      
        var startsAt = req.StartsAt ?? d.StartsAt;
        var endsAt   = req.EndsAt   ?? d.EndsAt;
        if (req.StartsAt.HasValue || req.EndsAt.HasValue)
        {
            if (startsAt >= endsAt)
                throw new ArgumentException("StartsAt must be before EndsAt");
            d.StartsAt = startsAt;
            d.EndsAt   = endsAt;
        }

        if (!string.IsNullOrWhiteSpace(req.Status))
            d.Status = NormalizeStatus(req.Status!);
    }

    
    private static string NormalizeType(string type)
    {
        var t = type.Trim();
        return t.Equals("percent", StringComparison.OrdinalIgnoreCase) ? "Percent" :
               t.Equals("amount",  StringComparison.OrdinalIgnoreCase) ? "Amount"  :
               t;
    }

    private static string NormalizeScope(string scope)
    {
        var s = scope.Trim();
        return s.Equals("order", StringComparison.OrdinalIgnoreCase) ? "Order" :
               s.Equals("line",  StringComparison.OrdinalIgnoreCase) ? "Line"  :
               s;
    }

    private static string NormalizeStatus(string status)
    {
        var s = status.Trim();
        return s.Equals("active",   StringComparison.OrdinalIgnoreCase) ? "Active"   :
               s.Equals("inactive", StringComparison.OrdinalIgnoreCase) ? "Inactive" :
               s;
    }

    
    public static IEnumerable<DiscountSummaryResponse> ToSummaryResponses(this IEnumerable<Discount> q)
        => q.Select(d => d.ToSummaryResponse());
}