namespace PsP.Contracts.Payments;

public class PaymentResponse
{
    public int PaymentId { get; set; }
    public long PaidByGiftCard { get; set; }
    public long RemainingForStripe { get; set; }
    public string? StripeUrl { get; set; }
    public string? StripeSessionId { get; set; }

    public PaymentResponse() {}

    public PaymentResponse(
        int paymentId,
        long paidByGiftCard,
        long remainingForStripe,
        string? stripeUrl,
        string? stripeSessionId)
    {
        PaymentId = paymentId;
        PaidByGiftCard = paidByGiftCard;
        RemainingForStripe = remainingForStripe;
        StripeUrl = stripeUrl;
        StripeSessionId = stripeSessionId;
    }
}