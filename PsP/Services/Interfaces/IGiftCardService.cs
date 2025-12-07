using PsP.Models;

namespace PsP.Services.Interfaces
{
    public interface IGiftCardService
    {
        // Backoffice / read
        Task<GiftCard?> GetByIdAsync(int id);
        Task<GiftCard?> GetByCodeAsync(string code);
        Task<List<GiftCard>> GetByBusinessAsync(int businessId, string? status = null, string? code = null);

        // Create
        Task<GiftCard> CreateAsync(GiftCard card);

        // Balance operations
        Task<bool> TopUpAsync(int id, long amount);

        // Business-safe redeem (privalomas businessId)
        Task<(long charged, long remaining)> RedeemAsync(int id, long amount, int businessId);

        // Deactivate
        Task<bool> DeactivateAsync(int id);
    }
}