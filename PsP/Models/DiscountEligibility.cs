namespace PsP.Models;

public class DiscountEligibility
{
    public int DiscountEligibilityId { get; set; }

    public int DiscountId { get; set; }
    public Discount Discount { get; set; } = null!;

    //public int CatalogItemId { get; set; }
    //public CatalogItem CatalogItem { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}