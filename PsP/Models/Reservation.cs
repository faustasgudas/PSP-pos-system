namespace PsP.Models;

public class Reservation
{
    public int ReservationId { get; set; }


    public int BusinessId { get; set; }
    public int EmployeeId { get; set; }       
    public int CatalogItemId { get; set; }    
    public DateTime BookedAt { get; set; }
    public DateTime AppointmentStart { get; set; }
    public int PlannedDurationMin { get; set; }
    public string Status { get; set; } = "Booked";   
    public string? Notes { get; set; }
    public string? TableOrArea { get; set; }   

   
    public Business? Business { get; set; }
    public Employee? Employee { get; set; }
    public CatalogItem? CatalogItem { get; set; }


}