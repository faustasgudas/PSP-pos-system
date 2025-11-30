using PsP.Contracts.Payments;
using PsP.Services.Implementations;

namespace PsP.Mappings;

public static class PaymentMappings
{
    public static PaymentResponse ToResponse(this PaymentResponse r) =>
        new PaymentResponse(
            r.PaymentId,
            r.PaidByGiftCard,
            r.RemainingForStripe,
            r.StripeUrl,
            r.StripeSessionId
        );
}