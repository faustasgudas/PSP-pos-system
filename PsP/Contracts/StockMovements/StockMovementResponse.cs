namespace PsP.Contracts.StockMovements;

public class StockMovementResponse
{
    public int StockMovementId { get; set; }
    public int StockItemId { get; set; }
    public string Type { get; set; } = null!;
    public decimal Delta { get; set; }
    public decimal? UnitCostSnapshot { get; set; }
    public int? OrderLineId { get; set; }
    public DateTime At { get; set; }
    public string? Note { get; set; }
}