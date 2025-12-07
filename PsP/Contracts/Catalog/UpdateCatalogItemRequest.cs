namespace PsP.Contracts.Catalog;

public class UpdateCatalogItemRequest
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Type { get; set; }
    public decimal? BasePrice { get; set; }
    public string? Status { get; set; }
    public int? DefaultDurationMin { get; set; }
    public string? TaxClass { get; set; }
}