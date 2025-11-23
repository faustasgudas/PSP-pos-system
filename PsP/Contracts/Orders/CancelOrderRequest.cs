namespace PsP.Contracts.Orders;

public class CancelOrderRequest
{
    public int EmployeeId { get; set; }          // who cancels
    public string? Reason { get; set; }
    
    
}