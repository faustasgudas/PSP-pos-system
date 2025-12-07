using PsP.Contracts.StockMovements;

namespace PsP.Services.Interfaces;

public interface IStockMovementService
{
    Task<IEnumerable<StockMovementResponse>> ListAsync(
        int businessId,
        int stockItemId,
        int callerEmployeeId,
        string? type,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken ct = default);

    Task<StockMovementResponse> GetByIdAsync(
        int businessId,
        int stockItemId,
        int movementId,
        int callerEmployeeId,
        CancellationToken ct = default);

    Task<StockMovementResponse> CreateAsync(
        int businessId,
        int stockItemId,
        int callerEmployeeId,
        CreateStockMovementRequest request,
        CancellationToken ct = default);
}

