using Microsoft.EntityFrameworkCore;
using PsP.Data;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations
{
    public class BusinessService : IBusinessService
    {
        private readonly AppDbContext _db;

        public BusinessService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Business>> GetAllAsync()
        {
            return await _db.Businesses
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Business?> GetByIdAsync(int id)
        {
            return await _db.Businesses
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BusinessId == id);
        }

        public async Task<Business> CreateAsync(Business business)
        {
            // jei nori, čia gali užtikrinti default statusą
            if (string.IsNullOrWhiteSpace(business.BusinessStatus))
                business.BusinessStatus = "Active";

            _db.Businesses.Add(business);
            await _db.SaveChangesAsync();
            return business;
        }

        public async Task<Business?> UpdateAsync(int id, Business updated)
        {
            var existing = await _db.Businesses.FindAsync(id);
            if (existing == null)
                return null;

            existing.Name = updated.Name;
            existing.Address = updated.Address;
            existing.Phone = updated.Phone;
            existing.Email = updated.Email;
            existing.CountryCode = updated.CountryCode;
            existing.PriceIncludesTax = updated.PriceIncludesTax;
            // BusinessStatus specialiai neliečiam – atskiras concern

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _db.Businesses.FindAsync(id);
            if (existing == null)
                return false;

            _db.Businesses.Remove(existing);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
