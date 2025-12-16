namespace PsP.Models;

public class Reservation
{
    public int ReservationId { get; set; }

    // FK
    public int BusinessId { get; set; }
    public int EmployeeId { get; set; }        // assigned staff for the service
    public int CatalogItemId { get; set; }     // the service being reserved (Type = "Service")
    public DateTime BookedAt { get; set; }
    public DateTime AppointmentStart { get; set; }
    public int PlannedDurationMin { get; set; }
    public string Status { get; set; } = "Booked";   // "Booked" / "Cancelled" / "Completed"
    public string? Notes { get; set; }
    public string? TableOrArea { get; set; }   // optional for café

    // Nav
    public Business? Business { get; set; }
    public Employee? Employee { get; set; }
    public CatalogItem? CatalogItem { get; set; }


}