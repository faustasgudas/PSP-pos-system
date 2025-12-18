namespace PsP.Contracts.Catalog;

public class CatalogItemSummaryResponse
{
    public int CatalogItemId { get; set; }
    public int BusinessId { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Type { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public string Status { get; set; } = null!;
    public string TaxClass { get; set; } = null!;
    public int DefaultDurationMin { get; set; }
}