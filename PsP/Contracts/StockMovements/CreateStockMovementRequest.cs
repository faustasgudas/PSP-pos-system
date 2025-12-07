namespace PsP.Contracts.StockMovements;

public class CreateStockMovementRequest
{
    public string Type { get; set; } = null!;          // "Receive" | "Sale" | "RefundReturn" | "Waste" | "Adjust"
    public decimal Delta { get; set; }                 // + in / - out
    public decimal? UnitCostSnapshot { get; set; }     // set on Receive/Adjust; null for Sales
    public int? OrderLineId { get; set; }              // when movement is tied to a sale/refund
    public DateTime? At { get; set; }                  // optional override; default now
    public string? Note { get; set; }
}