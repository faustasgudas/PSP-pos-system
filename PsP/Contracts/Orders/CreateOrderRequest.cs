namespace PsP.Contracts.Orders;

public class CreateOrderRequest
{
    
    public int EmployeeId { get; set; }          // who opens it
    public int? ReservationId { get; set; }      // optional link
    public string? TableOrArea { get; set; }
    
}