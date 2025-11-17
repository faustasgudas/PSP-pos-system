namespace PsP.Models;

public class Order
{
    public int OrderId { get; set; }

    
    public int BusinessId { get; set; }
    public int EmployeeId { get; set; }              
    public int? ReservationId { get; set; }      
    
    public string Status { get; set; } = "Open";

    public string? TableOrArea { get; set; }

    
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    
    public Decimal TipAmount { get; set; } = 0;

    
    public string? OrderDiscountSnapshot { get; set; }
    public int? DiscountId { get; set; }
    
}

