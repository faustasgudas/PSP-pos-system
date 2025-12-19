namespace PsP.Contracts.Catalog;

public class CreateCatalogItemRequest
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Type { get; set; } = null!;        
    public decimal BasePrice { get; set; }
    public string Status { get; set; } = "Active";     
    public int DefaultDurationMin { get; set; }      
    public string TaxClass { get; set; } = null!;
}