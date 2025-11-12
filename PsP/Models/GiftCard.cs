namespace PsP.Models;

public class GiftCard
{
    public int GiftCardId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = "Active";
    public int BusinessId { get; set; }
    public int? PaymentId { get; set; }
}