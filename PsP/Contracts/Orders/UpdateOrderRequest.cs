namespace PsP.Contracts.Orders;

public class UpdateOrderRequest
{
    
    public int EmployeeId { get; set; }
    public string? Status { get; set; }         
    public string? TableOrArea { get; set; }
    public string? TipAmount { get; set; }      
    public int? DiscountId { get; set; }
    
}