using PsP.Contracts.Payments;
using PsP.Models;

namespace PsP.Services.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> CreatePaymentAsync(
        int orderId,
        int businessId,
        int callerEmployeeId,
        string? giftCardCode,
        long? giftCardAmountCents,
        long? tipCents,
        string baseUrl,
        CancellationToken ct = default);

    Task ConfirmStripeSuccessAsync(string sessionId, CancellationToken ct = default);

    Task CancelStripeAsync(string sessionId, CancellationToken ct = default);

    Task RefundFullAsync(int paymentId, CancellationToken ct = default);

    Task<List<Payment>> GetPaymentsForOrderAsync(int businessId, int orderId);
    Task<List<Payment>> GetPaymentsForBusinessAsync(int businessId);
}