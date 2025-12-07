namespace PsP.Contracts.Catalog;

public class CreateCatalogItemRequest
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Type { get; set; } = null!;         // "Product" | "Service"
    public decimal BasePrice { get; set; }
    public string Status { get; set; } = "Active";     // "Draft" | "Active" | "Archived"
    public int DefaultDurationMin { get; set; }        // for services; 0 for products
    public string TaxClass { get; set; } = null!;
}