namespace PsP.Models;

public class StockMovement
{
    public int StockMovementId { get; set; }

   
    public int StockItemId { get; set; }
    public int? OrderLineId { get; set; }   

    public string Type { get; set; } = string.Empty;  
    public decimal Delta { get; set; }                
    public decimal? UnitCostSnapshot { get; set; }    
    public DateTime At { get; set; }

    
    public StockItem? StockItem { get; set; }
    
    public OrderLine? OrderLine { get; set; }
}