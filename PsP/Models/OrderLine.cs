namespace PsP.Models;

public class OrderLine
{
    
    // Keys / FK
    public int OrderLineId { get; set; }
    public int OrderId { get; set; }              // FK -> Order
    public int BusinessId { get; set; }           // denorm for scoping/queries
    public int CatalogItemId { get; set; }        // FK -> CatalogItem
    public int? DiscountId { get; set; }          // applied discount (optional)

    // Quantities
    public decimal Qty { get; set; }

    // Snapshots (immutable after capture)
    public string ItemNameSnapshot { get; set; } = string.Empty;
    public decimal UnitPriceSnapshot { get; set; }           // price at time of sale
    public string? UnitDiscountSnapshot { get; set; } // text/json of discount details (if any)
    
    public string CatalogTypeSnapshot { get; set; } = string.Empty;
    public string TaxClassSnapshot { get; set; } = string.Empty; // e.g. "Food" / "Service"
    public decimal TaxRateSnapshotPct { get; set; }          // e.g. 21.00m

    // Audit
    public DateTime PerformedAt { get; set; }                // when line was added/updated
    public int? PerformedByEmployeeId { get; set; }          // who did it (optional)

    public CatalogItem CatalogItem { get; set; } = null!;
    public Order Order { get; set; } = null!;
    public ICollection<StockMovement> StockMovement { get; set; } = new List<StockMovement>();

    
}