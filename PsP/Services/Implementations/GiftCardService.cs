using Microsoft.EntityFrameworkCore;
using PsP.Data;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations
{
    public class GiftCardService : IGiftCardService
    {
        private readonly AppDbContext _db;

        public GiftCardService(AppDbContext db)
        {
            _db = db;
        }

        public Task<GiftCard?> GetByIdAsync(int id)
            => _db.GiftCards.FirstOrDefaultAsync(x => x.GiftCardId == id);

        public Task<GiftCard?> GetByCodeAsync(string code)
            => _db.GiftCards.FirstOrDefaultAsync(x => x.Code == code);

        public async Task<GiftCard> CreateAsync(GiftCard card)
        {
            card.IssuedAt = DateTime.UtcNow;
            card.Status ??= "Active";

            _db.GiftCards.Add(card);
            await _db.SaveChangesAsync();

            return card;
        }

        public async Task<bool> TopUpAsync(int id, long amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            var c = await _db.GiftCards.FindAsync(id);
            if (c is null)
                return false;

            EnsureActiveAndNotExpired(c);

            c.Balance += amount;
            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<(long charged, long remaining)> RedeemAsync(int id, long amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            var c = await _db.GiftCards.FindAsync(id)
                    ?? throw new KeyNotFoundException("not_found");

            EnsureActiveAndNotExpired(c);

            var charge = Math.Min(c.Balance, amount);
            if (charge == 0)
                return (0, c.Balance);

            c.Balance -= charge;
            await _db.SaveChangesAsync();

            return (charge, c.Balance);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var c = await _db.GiftCards.FindAsync(id);
            if (c is null)
                return false;

            if (c.Status == "Inactive")
                return true; // jau išjungta, laikom kaip success

            c.Status = "Inactive";
            await _db.SaveChangesAsync();

            return true;
        }

        // --- helperis bendrai validacijai ---
        private static void EnsureActiveAndNotExpired(GiftCard c)
        {
            if (c.Status != "Active")
                throw new InvalidOperationException("blocked");

            if (c.ExpiresAt is not null && c.ExpiresAt <= DateTime.UtcNow)
                throw new InvalidOperationException("expired");
        }
    }
}
