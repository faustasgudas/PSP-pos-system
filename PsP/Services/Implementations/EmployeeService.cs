using Microsoft.EntityFrameworkCore;
using PsP.Data;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations;

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _db;

    public EmployeeService(AppDbContext db)
    {
        _db = db;
    }
    private async Task<Employee> EnsureCallerIsManagerOrOwnerAsync(
        int businessId,
        int callerEmployeeId)
    {
        var caller = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == callerEmployeeId);

        if (caller is null || caller.BusinessId != businessId)
            throw new InvalidOperationException("caller_not_found_or_wrong_business");

        if (!caller.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("caller_inactive");

        var isOwner = caller.Role.Equals("Owner", StringComparison.OrdinalIgnoreCase);
        var isManager = caller.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase);

        if (!isOwner && !isManager)
            throw new InvalidOperationException("forbidden"); 

        return caller;
    }

    private static IQueryable<Employee> ApplyFilters(
        IQueryable<Employee> query,
        string? role,
        string? status)
    {
        if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim();
            query = query.Where(e => e.Role == r);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim();
            query = query.Where(e => e.Status == s);
        }

        return query;
    }

    public async Task<List<Employee>> GetAllAsync(
        int businessId,
        int callerEmployeeId,
        string? role = null,
        string? status = null)
    {
        

        var query = _db.Employees
            .AsNoTracking()
            .Where(e => e.BusinessId == businessId);

        query = ApplyFilters(query, role, status);

        return await query
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<Employee?> GetByIdAsync(
        int businessId,
        int employeeId,
        int callerEmployeeId)
    {
       
        return await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.EmployeeId == employeeId &&
                e.BusinessId == businessId);
    }

    public async Task<Employee> CreateAsync(
        int businessId,
        int callerEmployeeId,
        Employee employee)
    {
        await EnsureCallerIsManagerOrOwnerAsync(businessId, callerEmployeeId);

        
        employee.BusinessId = businessId;

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        return employee;
    }

    public async Task<Employee?> UpdateAsync(
        int businessId,
        int employeeId,
        int callerEmployeeId,
        Employee updated)
    {
        await EnsureCallerIsManagerOrOwnerAsync(businessId, callerEmployeeId);

        var existing = await _db.Employees
            .FirstOrDefaultAsync(e =>
                e.EmployeeId == employeeId &&
                e.BusinessId == businessId);

        if (existing is null)
            return null;

        existing.Name = updated.Name;
        existing.Role = updated.Role;
        existing.Status = updated.Status;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<Employee?> ChangeStatusAsync(
        int businessId,
        int employeeId,
        int callerEmployeeId,
        string newStatus)
    {
        await EnsureCallerIsManagerOrOwnerAsync(businessId, callerEmployeeId);

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e =>
                e.EmployeeId == employeeId &&
                e.BusinessId == businessId);

        if (employee is null)
            return null;

        if (!string.IsNullOrWhiteSpace(newStatus))
        {
         
            var s = newStatus.Trim();
            employee.Status = s;
        }

        await _db.SaveChangesAsync();
        return employee;
    }
    
    public async Task<Employee?> DeactivateAsync(
        int businessId,
        int employeeId,
        int callerEmployeeId,
        string? reason)
    {
        await EnsureCallerIsManagerOrOwnerAsync(businessId, callerEmployeeId);

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e =>
                e.EmployeeId == employeeId &&
                e.BusinessId == businessId);

        if (employee is null)
            return null;

        
        employee.Status = "Terminated";

       

        await _db.SaveChangesAsync();
        return employee;
    }
}
