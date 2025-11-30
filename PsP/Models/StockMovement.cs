namespace PsP.Models;

public class StockMovement
{
    public int StockMovementId { get; set; }

    // FK
    public int StockItemId { get; set; }
    public int? OrderLineId { get; set; }   // audit link for sale/refund movements

    public string Type { get; set; } = string.Empty;  // "Receive" / "Sale" / "RefundReturn" / "Waste" / "Adjust"
    public decimal Delta { get; set; }                // +in / -out
    public decimal? UnitCostSnapshot { get; set; }    // set on Receive; optional otherwise
    public DateTime At { get; set; }

    // Nav
    public StockItem? StockItem { get; set; }
    
    public OrderLine? OrderLine { get; set; }
}