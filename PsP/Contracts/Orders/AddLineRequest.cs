namespace PsP.Contracts.Orders;

public class AddLineRequest
{
    public int CatalogItemId { get; set; }      
    public decimal Qty { get; set; }
    
}