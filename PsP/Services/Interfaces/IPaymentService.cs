using PsP.Contracts.Payments;
using PsP.Models;

namespace PsP.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponse> CreatePaymentAsync(
            int orderId,
            string currency,
            int businessId,
            string? giftCardCode,
            long? giftCardAmountCents,
            string baseUrl);

        Task ConfirmStripeSuccessAsync(string sessionId);
        Task RefundFullAsync(int paymentId);
        Task<List<Payment>> GetPaymentsForOrderAsync(int businessId, int orderId);
        Task<List<Payment>> GetPaymentsForBusinessAsync(int businessId);
    }
}