namespace PsP.Models;

public class OrderLine
{
    
    
    public int OrderLineId { get; set; }
    public int OrderId { get; set; }              
    public int BusinessId { get; set; }           
    public int CatalogItemId { get; set; }        
    public int? DiscountId { get; set; }          

 
    public decimal Qty { get; set; }

   
    public string ItemNameSnapshot { get; set; } = string.Empty;
    public decimal UnitPriceSnapshot { get; set; }          
    public string? UnitDiscountSnapshot { get; set; } 
    
    public string CatalogTypeSnapshot { get; set; } = string.Empty;
    public string TaxClassSnapshot { get; set; } = string.Empty; 
    public decimal TaxRateSnapshotPct { get; set; }        

   
    public DateTime PerformedAt { get; set; }                
    public int? PerformedByEmployeeId { get; set; }         

    public CatalogItem CatalogItem { get; set; } = null!;
    public Order Order { get; set; } = null!;
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    
}