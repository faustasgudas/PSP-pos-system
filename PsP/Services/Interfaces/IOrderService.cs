using PsP.Contracts.Orders;

namespace PsP.Services.Interfaces;


public interface IOrdersService
{
    Task<OrderDetailResponse> CreateOrderAsync(
        int businessId,
        CreateOrderRequest request,
       
        CancellationToken ct = default);




     Task<IEnumerable<OrderSummaryResponse>> ListAllAsync(
         int businessId,
         int callerEmployeeId,
         string? status,
         DateTime? from,
         DateTime? to,
         CancellationToken ct = default);

     Task<IEnumerable<OrderSummaryResponse>> ListMineAsync(
         int businessId,
         int callerEmployeeId,
         CancellationToken ct = default);

     Task<OrderDetailResponse> GetOrderAsync(
         int businessId,
         int orderId,
         int callerEmployeeId,
         CancellationToken ct = default);

     Task<IEnumerable<OrderLineResponse>> ListLinesAsync(
         int businessId,
         int orderId,
         int callerEmployeeId,
         CancellationToken ct = default);

     Task<OrderLineResponse> GetLineAsync(
         int businessId,
         int orderId,
         int orderLineId,
         int callerEmployeeId,
         CancellationToken ct = default);
     
     Task<OrderDetailResponse> UpdateOrderAsync(
         int businessId,
         int orderId,
         int callerEmployeeId,
         UpdateOrderRequest request,
        
         CancellationToken ct = default);

     Task<OrderDetailResponse> CloseOrderAsync(
         int businessId,
         int orderId,
         int callerEmployeeId,
         CancellationToken ct = default);

     Task<OrderDetailResponse> CancelOrderAsync(
         int businessId,
         int orderId,
         int callerEmployeeId,
         CancelOrderRequest request,
         CancellationToken ct = default);

     Task<OrderLineResponse> AddLineAsync(
         int businessId,
         int orderId,
         int callerEmployeeId,
         AddLineRequest request,
        
         CancellationToken ct = default);

     Task<OrderLineResponse> UpdateLineAsync(
         int businessId,
         int orderId,
         int orderLineId,
         int callerEmployeeId,
         UpdateLineRequest request,
         
         CancellationToken ct = default);

     Task RemoveLineAsync(
         int businessId,
         int orderId,
         int orderLineId,
         int callerEmployeeId,
         CancellationToken ct = default);

}