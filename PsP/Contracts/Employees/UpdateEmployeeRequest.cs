namespace PsP.Contracts.Employees;

public class UpdateEmployeeRequest
{
    public string? Name { get; set; }
    
    public string Email { get; set; } = null!;
    
    public string Password { get; set; } = null!;
    public string? Role { get; set; }
    public string? Status { get; set; }
}