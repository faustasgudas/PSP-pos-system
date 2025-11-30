namespace PsP.Contracts.StockItems;

public class StockItemDetailResponse : StockItemSummaryResponse
{
    public decimal AverageUnitCost { get; set; }
}
