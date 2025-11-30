namespace PsP.Contracts.StockItems;

public class CreateStockItemRequest
{
    public int CatalogItemId { get; set; }
    public string Unit { get; set; } = null!;          // "pcs" | "ml" | "g"
    public decimal? InitialQtyOnHand { get; set; }     // optional initial quantity
    public decimal? InitialAverageUnitCost { get; set; } 
}