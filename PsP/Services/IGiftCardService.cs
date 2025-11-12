using PsP.Models;

namespace PsP.Services
{
    public interface IGiftCardService
    {
        Task<GiftCard?> GetByIdAsync(int id);              
        Task<GiftCard?> ValidateAsync(string code);
        Task<GiftCard> CreateAsync(GiftCard card);
        Task<bool> TopUpAsync(int id, decimal amount);
        Task<(decimal charged, decimal remaining)> RedeemAsync(string code, decimal amount);
    }
}