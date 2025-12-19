namespace PsP.Models;

public class Payment
{
    public int PaymentId { get; set; }

    public long AmountCents { get; set; }
    public long TipCents { get; set; } = 0;

    public string Currency { get; set; } = "EUR";
    public string Method { get; set; } = "Stripe";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? RefundedAt { get; set; }

    public string Status { get; set; } = "Pending"; // Pending|Success|Cancelled|Refunded|Failed
    public bool IsOpen { get; set; } = true;        // 👈 for unique constraint
    
    public bool InventoryApplied { get; set; } = false;
    
    public DateTime? InventoryAppliedAt { get; set; }
    public string? StripeSessionId { get; set; }

    public int? GiftCardId { get; set; }
    public GiftCard? GiftCard { get; set; }

    public long GiftCardPlannedCents { get; set; } = 0;
    public long GiftCardChargedCents { get; set; } = 0; // 👈 actual charged (refund uses this)

    public int BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}
