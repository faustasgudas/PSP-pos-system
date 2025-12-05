namespace PsP.Contracts.Orders;

public class AddLineRequest
{
    public int CatalogItemId { get; set; }       // points to CatalogItem
    public decimal Qty { get; set; }
    
}