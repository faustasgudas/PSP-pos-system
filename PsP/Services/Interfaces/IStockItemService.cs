using PsP.Contracts.StockItems;

namespace PsP.Services.Interfaces;

public interface IStockItemService
{
    Task<IEnumerable<StockItemSummaryResponse>> ListAsync(
        int businessId,
        int callerEmployeeId,
        int? catalogItemId,
        CancellationToken ct = default);

    Task<StockItemDetailResponse?> GetOneAsync(
        int businessId,
        int stockItemId,
        int callerEmployeeId,
        CancellationToken ct = default);

    Task<StockItemDetailResponse> CreateAsync(
        int businessId,
        int callerEmployeeId,
        CreateStockItemRequest request,
        CancellationToken ct = default);

    Task<StockItemDetailResponse?> UpdateAsync(
        int businessId,
        int stockItemId,
        int callerEmployeeId,
        UpdateStockItemRequest request,
        CancellationToken ct = default);
}