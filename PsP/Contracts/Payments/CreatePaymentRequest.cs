using System.ComponentModel.DataAnnotations;

namespace PsP.Contracts.Payments;

public class CreatePaymentRequest
{
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }

    public string? GiftCardCode { get; set; }

    [Range(1, long.MaxValue)]
    public long? GiftCardAmountCents { get; set; }

    [Range(0, long.MaxValue)]
    public long? TipCents { get; set; } // optional
}