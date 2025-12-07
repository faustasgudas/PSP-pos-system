namespace PsP.Contracts.StockItems;

public class UpdateStockItemRequest
{
    public string? Unit { get; set; }
// qty and costs change via StockMovements, not here
}