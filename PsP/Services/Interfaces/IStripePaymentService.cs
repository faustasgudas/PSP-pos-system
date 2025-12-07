using System.Threading;
using System.Threading.Tasks;
using Stripe.Checkout;

namespace PsP.Services.Interfaces
{
    public interface IStripePaymentService
    {
        Session CreateCheckoutSession(
            long amountCents,
            string currency,
            string successUrl,
            string cancelUrl,
            int paymentId);

        Task RefundAsync(
            string stripeSessionId,
            long amountCents,
            CancellationToken ct = default);
    }
}