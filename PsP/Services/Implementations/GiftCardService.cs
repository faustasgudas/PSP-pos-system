using Microsoft.EntityFrameworkCore;
using PsP.Data;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations
{
    public class GiftCardService : IGiftCardService
    {
        private readonly AppDbContext _db;
        private const int MaxConcurrencyRetries = 5;

        public GiftCardService(AppDbContext db)
        {
            _db = db;
        }

        // ========== READ ==========

        public Task<GiftCard?> GetByIdAsync(int id) =>
            _db.GiftCards
               .AsNoTracking()
               .FirstOrDefaultAsync(x => x.GiftCardId == id);

        public Task<GiftCard?> GetByCodeAsync(string code) =>
            _db.GiftCards
               .AsNoTracking()
               .FirstOrDefaultAsync(x => x.Code == code);

        public async Task<List<GiftCard>> GetByBusinessAsync(
            int businessId,
            string? status = null,
            string? code = null)
        {
            var query = _db.GiftCards
                .AsNoTracking()
                .Where(g => g.BusinessId == businessId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim();
                query = query.Where(g => g.Status == s);
            }

            if (!string.IsNullOrWhiteSpace(code))
            {
                var c = code.Trim();
                query = query.Where(g => g.Code.Contains(c));
            }

            return await query
                .OrderByDescending(g => g.IssuedAt)
                .ToListAsync();
        }

        // ========== CREATE ==========

        public async Task<GiftCard> CreateAsync(GiftCard card)
        {
            card.IssuedAt = DateTime.UtcNow;
            card.Status ??= "Active";

            _db.GiftCards.Add(card);
            await _db.SaveChangesAsync();

            return card;
        }

        // ========== TOP UP ==========

        public async Task<bool> TopUpAsync(int id, long amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
            {
                var c = await _db.GiftCards
                    .FirstOrDefaultAsync(x => x.GiftCardId == id);

                if (c is null)
                    return false;

                EnsureActiveAndNotExpired(c);

                c.Balance += amount;

                try
                {
                    await _db.SaveChangesAsync();
                    return true;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (attempt == MaxConcurrencyRetries - 1)
                        throw new InvalidOperationException("concurrency_conflict", ex);

                    foreach (var entry in ex.Entries)
                        await entry.ReloadAsync();
                }
            }

            return false;
        }

        // ========== REDEEM ==========

        public async Task<(long charged, long remaining)> RedeemAsync(
            int id,
            long amount,
            int businessId)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
            {
                var c = await _db.GiftCards
                    .FirstOrDefaultAsync(x => x.GiftCardId == id)
                        ?? throw new KeyNotFoundException("not_found");

                EnsureActiveAndNotExpired(c);

                if (c.BusinessId != businessId)
                    throw new InvalidOperationException("wrong_business");

                var charge = Math.Min(c.Balance, amount);
                if (charge == 0)
                    return (0, c.Balance);

                c.Balance -= charge;

                try
                {
                    await _db.SaveChangesAsync();
                    return (charge, c.Balance);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (attempt == MaxConcurrencyRetries - 1)
                        throw new InvalidOperationException("concurrency_conflict", ex);

                    foreach (var entry in ex.Entries)
                        await entry.ReloadAsync();
                }
            }

            // neturėtume čia nueit
            return (0, 0);
        }

        // ========== DEACTIVATE ==========

        public async Task<bool> DeactivateAsync(int id)
        {
            var c = await _db.GiftCards.FirstOrDefaultAsync(x => x.GiftCardId == id);
            if (c is null)
                return false;

            if (c.Status == "Inactive")
                return true;

            c.Status = "Inactive";
            await _db.SaveChangesAsync();

            return true;
        }

        // ========== VALIDACIJA ==========

        private static void EnsureActiveAndNotExpired(GiftCard c)
        {
            if (!string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("blocked");

            if (c.ExpiresAt is not null && c.ExpiresAt <= DateTime.UtcNow)
                throw new InvalidOperationException("expired");
        }
    }
}
