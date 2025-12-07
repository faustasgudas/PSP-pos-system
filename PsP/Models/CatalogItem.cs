namespace PsP.Models;

public class CatalogItem
{
    public int CatalogItemId { get; set; }

    // FK
    public int BusinessId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;   // SKU / service code
    public string Type { get; set; } = string.Empty;   // "Product" / "Service"
    public decimal BasePrice { get; set; }
    public string Status { get; set; } = string.Empty; // "Draft" / "Active" / "Archived"
    public int DefaultDurationMin { get; set; }        // used when Type = "Service"
    public string TaxClass { get; set; } = string.Empty; // "Food" / "Service" / "Alcohol" / "Other"

    // Nav
    public Business? Business { get; set; }
    public StockItem? StockItem { get; set; }          // 1:0..1
    public ICollection<DiscountEligibility> DiscountEligibilities { get; set; } = new List<DiscountEligibility>();


    
}