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


}