namespace PsP.Contracts.Orders;

public class UpdateOrderRequest
{
    
    public int EmployeeId { get; set; }
    public string? Status { get; set; }          // "Open" | "Cancelled" (keep Open-only here)
    public string? TableOrArea { get; set; }
    public string? TipAmount { get; set; }       // "12.50"
    public int? DiscountId { get; set; }
    
}