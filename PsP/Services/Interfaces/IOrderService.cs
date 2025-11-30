using PsP.Contracts.Orders;

namespace PsP.Services.Interfaces;


    public interface IOrdersService
    {
        Task<OrderSummaryResponse> CreateOrderAsync(
            int businessId,
            CreateOrderRequest request,
            CancellationToken ct = default);
    }
