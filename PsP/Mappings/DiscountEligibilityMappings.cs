using PsP.Contracts.Discounts;
using PsP.Models;

namespace PsP.Mappings;

public static class DiscountEligibilityMappings
{
    public static DiscountEligibilityResponse ToResponse(this DiscountEligibility e) => new()
    {
        DiscountId    = e.DiscountId,
        CatalogItemId = e.CatalogItemId,
        CreatedAt     = e.CreatedAt
    };

    // ===== Request -> Entity (create) =====
    // Note: usually DiscountId is in route; CatalogItemId in body.
    public static DiscountEligibility ToNewEntity(this CreateDiscountEligibilityRequest req, int discountId)
    {
        if (req.CatalogItemId <= 0) throw new ArgumentException("CatalogItemId is required");
        return new DiscountEligibility
        {
            DiscountId    = discountId,
            CatalogItemId = req.CatalogItemId,
            // CreatedAt set by DB default (CURRENT_TIMESTAMP) per your model config
        };
    }

    // No Update() for eligibilities (composite key row). To "change" you typically delete + recreate.
    // Convenience for lists:
    public static IEnumerable<DiscountEligibilityResponse> ToResponses(this IEnumerable<DiscountEligibility> q)
        => q.Select(e => e.ToResponse());
}