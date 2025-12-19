namespace PsP.Contracts.Orders;

public class CancelOrderRequest
{
    public int EmployeeId { get; set; }        
    public string? Reason { get; set; }
    
    
}