namespace PsP.Contracts.Orders;

public class CreateOrderRequest
{
    
    public int EmployeeId { get; set; }       
    public int? ReservationId { get; set; }  
    public string? TableOrArea { get; set; }
    
}