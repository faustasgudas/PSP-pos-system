using System.ComponentModel.DataAnnotations;

namespace PsP.Contracts.Payments;

public class CreatePaymentRequest
{
    [Range(1, long.MaxValue)]
    public long AmountCents { get; set; }

    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "eur";

    [Range(1, int.MaxValue)]
    public int BusinessId { get; set; }

    public string? GiftCardCode { get; set; }

    [Range(1, long.MaxValue)]
    public long? GiftCardAmountCents { get; set; }
}