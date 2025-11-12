namespace PsP.Models;

public class Payment
{
    public int PaymentId { get; set; }
    public long AmountCents { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Method { get; set; } = string.Empty;
    public decimal? TipPortionCents { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    //public int? OrderId { get; set; }
    public int? GiftCardId { get; set; }
    public int? EmployeeId { get; set; }
    
    //public Order? Order { get; set; }
    public GiftCard? GiftCard { get; set; }
    //public Employee? Employee { get; set; }
}
