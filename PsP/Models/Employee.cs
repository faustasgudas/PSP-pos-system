namespace PsP.Models;

public class Employee
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    
    public string PasswordHash { get; set; } = null!;
    
    
    public string Role { get; set; } = null!;

 
    public string Status { get; set; } = "Active";

    public int BusinessId { get; set; } 
    public Business  Business { get; set; } 

    public ICollection<Order> Orders { get; set; } = new List<Order>();         
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}