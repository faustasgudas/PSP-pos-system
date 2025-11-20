using PsP.Models;

namespace PsP.Services.Interfaces;

public interface IBusinessService
{
    Task<List<Business>> GetAllAsync();
    Task<Business?> GetByIdAsync(int id);
    Task<Business> CreateAsync(Business business);
    Task<Business?> UpdateAsync(int id, Business updated);
    Task<bool> DeleteAsync(int id);
}