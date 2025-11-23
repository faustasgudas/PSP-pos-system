namespace PsP.Models;

public class Employee
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = null!;

    // Roles: Owner, Manager, Staff
    public string Role { get; set; } = null!;

    // Active, OnLeave, Terminated
    public string BusinessStatus { get; set; } = "Active";

    public int BusinessId { get; set; } 
}