namespace PsP.Models;

public class Payment
{
    public int PaymentId { get; set; }

    public long AmountCents { get; set; }
    public string Currency { get; set; } = "eur";
    public string Method { get; set; } = "Stripe";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Created";

    public string? StripeSessionId { get; set; }

    public int? GiftCardId { get; set; }
    public GiftCard? GiftCard { get; set; }

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    public long GiftCardPlannedCents { get; set; }
}
