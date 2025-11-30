namespace PsP.Contracts.Employees;

public class CreateEmployeeRequest
{
    public string Name { get; set; } = null!;
    public string Role { get; set; } = null!;     // "Owner" | "Manager" | "Staff"
    public string Status { get; set; } = "Active"; // "Active" | "OnLeave" | "Terminated"
}