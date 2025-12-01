using PsP.Models;

namespace PsP.Services.Interfaces;

public interface IEmployeeService
{
    Task<List<Employee>> GetAllAsync(int businessId, int callerEmployeeId, string? role = null, string? status = null);
    Task<Employee?> GetByIdAsync(int businessId, int employeeId, int callerEmployeeId);
    Task<Employee> CreateAsync(int businessId, int callerEmployeeId, Employee employee);
    Task<Employee?> UpdateAsync(int businessId, int employeeId, int callerEmployeeId, Employee updated);
    Task<Employee?> DeactivateAsync(int businessId, int employeeId, int callerEmployeeId, string? reason);
}
