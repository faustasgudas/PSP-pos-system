using PsP.Contracts.Catalog;
using PsP.Contracts.Discounts;
using PsP.Contracts.Snapshots;
using PsP.Models;

namespace PsP.Services.Interfaces;

public interface IDiscountsService
{
    Task<Discount?> GetNewestOrderDiscountAsync(
        int businessId,
        DateTime? nowUtc = null,
        CancellationToken ct = default);

    
    Task<Discount?> GetNewestLineDiscountForItemAsync(
        int businessId,
        int catalogItemId,
        DateTime? nowUtc = null,
        CancellationToken ct = default);

    Task<Discount> EnsureLineDiscountEligibleAsync(
        int businessId,
        int discountId,
        int catalogItemId,
        DateTime? nowUtc = null,
        CancellationToken ct = default);

    Task<Discount> EnsureOrderDiscountEligibleAsync(
        int businessId,
        int discountId,
        DateTime? nowUtc = null,
        CancellationToken ct = default);
   
    
    string MakeOrderDiscountSnapshot(Discount d, DateTime? capturedAtUtc = null);
    string MakeLineDiscountSnapshot(Discount d, int catalogItemId, DateTime? capturedAtUtc = null);
    DiscountSnapshot? TryParseDiscountSnapshot(string? json);



    Task<Discount?> GetOrderDiscountAsync(
        int discountId,
        CancellationToken ct = default);
   Task<IEnumerable<DiscountSummaryResponse>> ListDiscountsAsync(
           int businessId, int callerId, CancellationToken ct = default);
   
       Task<DiscountDetailResponse> GetDiscountAsync(
           int businessId, int callerId, int discountId, CancellationToken ct = default);
   
       Task<DiscountDetailResponse> CreateDiscountAsync(
           int businessId, int callerId, CreateDiscountRequest body, CancellationToken ct = default);
   
       Task<DiscountDetailResponse> UpdateDiscountAsync(
           int businessId, int callerId, int discountId, UpdateDiscountRequest body, CancellationToken ct = default);
   
       Task DeleteDiscountAsync(
           int businessId, int callerId, int discountId, CancellationToken ct = default);
   
       Task<IEnumerable<DiscountEligibilityResponse>> ListEligibilitiesAsync(
           int businessId, int callerId, int discountId, CancellationToken ct = default);
   
       Task<DiscountEligibilityResponse> AddEligibilityAsync(
           int businessId, int callerId, int discountId, CreateDiscountEligibilityRequest body, CancellationToken ct = default);
   
       Task RemoveEligibilityAsync(
           int businessId, int callerId, int discountId, int catalogItemId, CancellationToken ct = default);

       Task<IEnumerable<CatalogItemSummaryResponse>> ListEligibleItemsAsync(
           int businessId, int callerId, int discountId, CancellationToken ct = default);




}