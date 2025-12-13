namespace PsP.Contracts.Employees;

public class EmployeeSummaryResponse
{
    public int EmployeeId { get; set; }
    public int BusinessId { get; set; }
    public string Name { get; set; } = null!;
    
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
    
}