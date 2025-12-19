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

 
    public static DiscountEligibility ToNewEntity(this CreateDiscountEligibilityRequest req, int discountId)
    {
        if (req.CatalogItemId <= 0) throw new ArgumentException("CatalogItemId is required");
        return new DiscountEligibility
        {
            DiscountId    = discountId,
            CatalogItemId = req.CatalogItemId,
            
        };
    }

  
    public static IEnumerable<DiscountEligibilityResponse> ToResponses(this IEnumerable<DiscountEligibility> q)
        => q.Select(e => e.ToResponse());
}