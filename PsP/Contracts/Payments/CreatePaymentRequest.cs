using System.ComponentModel.DataAnnotations;

namespace PsP.Contracts.Payments;

public class CreatePaymentRequest
{
    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "EUR";

    
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }

   
    public string? GiftCardCode { get; set; }

    [Range(1, long.MaxValue)]
    public long? GiftCardAmountCents { get; set; }
}