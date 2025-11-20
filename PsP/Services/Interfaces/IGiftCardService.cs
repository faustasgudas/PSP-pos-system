using PsP.Models;

namespace PsP.Services.Interfaces
{
    public interface IGiftCardService
    {
        Task<GiftCard?> GetByIdAsync(int id);
        Task<GiftCard?> GetByCodeAsync(string code);

        Task<GiftCard> CreateAsync(GiftCard card);

        Task<bool> TopUpAsync(int id, long amount);

        Task<(long charged, long remaining)> RedeemAsync(int id, long amount);

        Task<bool> DeactivateAsync(int id);
    }
}