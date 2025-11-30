using PsP.Contracts.Employees;
using PsP.Models;

namespace PsP.Mappings;

public static class EmployeeMappings
{
       // Entity -> Responses
    public static EmployeeSummaryResponse ToSummaryResponse(this Employee e) => new()
    {
        EmployeeId = e.EmployeeId,
        BusinessId = e.BusinessId,
        Name       = e.Name,
        Role       = e.Role,
        Status     = e.Status
    };
    
    // Request -> Entity
    public static Employee ToNewEntity(this CreateEmployeeRequest req, int businessId)
    {
        var name = req.Name?.Trim() ?? throw new ArgumentException("Name is required");
        var role = NormalizeRole(req.Role);
        var status = NormalizeEmployeeStatus(req.Status ?? "Active");

        return new Employee
        {
            BusinessId = businessId,
            Name       = name,
            Role       = role,
            Status     = status
        };
    }

    // Apply partial update
    public static void ApplyUpdate(this UpdateEmployeeRequest req, Employee e)
    {
        if (!string.IsNullOrWhiteSpace(req.Name))
            e.Name = req.Name.Trim();

        if (!string.IsNullOrWhiteSpace(req.Role))
            e.Role = NormalizeRole(req.Role!);

        if (!string.IsNullOrWhiteSpace(req.Status))
            e.Status = NormalizeEmployeeStatus(req.Status!);
    }

    private static string NormalizeRole(string role)
    {
        var r = role.Trim();
        return r.Equals("owner", StringComparison.OrdinalIgnoreCase)   ? "Owner"   :
               r.Equals("manager", StringComparison.OrdinalIgnoreCase) ? "Manager" :
               r.Equals("staff", StringComparison.OrdinalIgnoreCase)   ? "Staff"   :
               r; // leave as-is, DB/validation layer can reject unknowns
    }

    private static string NormalizeEmployeeStatus(string status)
    {
        var s = status.Trim();
        return s.Equals("active", StringComparison.OrdinalIgnoreCase)     ? "Active"     :
               s.Equals("onleave", StringComparison.OrdinalIgnoreCase)    ? "OnLeave"    :
               s.Equals("terminated", StringComparison.OrdinalIgnoreCase) ? "Terminated" :
               s;
    }
}