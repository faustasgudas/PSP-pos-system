namespace PsP.Contracts.Employees;

public class CreateEmployeeRequest
{
    public string Name { get; set; } = null!;
    
    public string Email { get; set; } = null!;
    
    public string Password { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string Status { get; set; } = "Active";
}
