namespace PsP.Models;

public class StockItem
{
    public int StockItemId { get; set; }

    
    public int CatalogItemId { get; set; }

    public string Unit { get; set; } = "pcs";  
    public decimal QtyOnHand { get; set; }
    public decimal AverageUnitCost { get; set; }

    
    public CatalogItem? CatalogItem { get; set; }
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

}