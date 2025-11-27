namespace PsP.Models;

public class StockItem
{
    public int StockItemId { get; set; }

    // FK (1:0..1 from CatalogItem)
    public int CatalogItemId { get; set; }

    public string Unit { get; set; } = "pcs";  // "pcs" / "ml" / "g"
    public decimal QtyOnHand { get; set; }
    public decimal AverageUnitCost { get; set; }

    // Nav
    public CatalogItem? CatalogItem { get; set; }
    public ICollection<StockMovement> StockMovement { get; set; } = new List<StockMovement>();

}