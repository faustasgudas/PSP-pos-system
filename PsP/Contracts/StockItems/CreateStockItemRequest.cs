namespace PsP.Contracts.StockItems;

public class CreateStockItemRequest
{
    public int CatalogItemId { get; set; }
    public string Unit { get; set; } = null!;          // "pcs" | "ml" | "g"
    public decimal? InitialQtyOnHand { get; set; } 
    public decimal? InitialAverageUnitCost { get; set; } 
}