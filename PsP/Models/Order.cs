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
    // public decimal? OrderDiscountValueSnapshot { get; set; }
    public int? DiscountId { get; set; }
    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
    
    public ICollection<Payment> Payments { get; set; } = new List<Payment>(); // ← add this

    public Business Business { get; set; } = null!;
    
    public Employee? Employee { get; set; }
    
    public Reservation? Reservation { get; set; }
}

