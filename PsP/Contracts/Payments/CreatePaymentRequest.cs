using System.ComponentModel.DataAnnotations;

namespace PsP.Contracts.Payments;

public class CreatePaymentRequest
{
    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "EUR";

    // Order, kuriam darom payment
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }

    // Optional: gift card
    public string? GiftCardCode { get; set; }

    [Range(1, long.MaxValue)]
    public long? GiftCardAmountCents { get; set; }
}