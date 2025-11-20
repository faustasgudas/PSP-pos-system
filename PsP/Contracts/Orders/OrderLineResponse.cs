namespace PsP.Contracts.Orders;

public class OrderLineResponse
{
    public int OrderLineId { get; set; }
    public int OrderId { get; set; }
    public int BusinessId { get; set; }
    public int CatalogItemId { get; set; }
    public int? DiscountId { get; set; }
    public decimal Qty { get; set; }

    public string ItemNameSnapshot { get; set; } = "";
    public decimal UnitPriceSnapshot { get; set; }
    public string? UnitDiscountSnapshot { get; set; }
    public string TaxClassSnapshot { get; set; } = "";
    public decimal TaxRateSnapshotPct { get; set; }

    public DateTime PerformedAt { get; set; }
    public int? PerformedByEmployeeId { get; set; }
}