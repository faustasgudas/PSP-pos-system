namespace PsP.Contracts.StockItems;

public class StockItemSummaryResponse
{
    public int StockItemId { get; set; }
    public int CatalogItemId { get; set; }
    public string Unit { get; set; } = null!;
    public decimal QtyOnHand { get; set; }
}