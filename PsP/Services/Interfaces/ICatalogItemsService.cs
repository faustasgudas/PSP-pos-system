using PsP.Contracts.Catalog;

namespace PsP.Services.Interfaces;

public interface ICatalogItemsService
{
    Task<IEnumerable<CatalogItemSummaryResponse>> ListAllAsync(int businessId, int callerEmployeeId, string? type, string? status, string? code);
    Task<CatalogItemDetailResponse?> GetByIdAsync(int businessId, int catalogItemId, int callerEmployeeId);
    Task<CatalogItemDetailResponse> CreateAsync(int businessId, int callerEmployeeId, CreateCatalogItemRequest body);
    Task<CatalogItemDetailResponse?> UpdateAsync(int businessId, int catalogItemId, int callerEmployeeId, UpdateCatalogItemRequest body);
    Task<bool> ArchiveAsync(int businessId, int catalogItemId, int callerEmployeeId);
}
