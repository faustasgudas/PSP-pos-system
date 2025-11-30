namespace PsP.Contracts.Reservations;

public class CreateReservationRequest
{
    public int EmployeeId { get; set; }            // assigned staff
    public int CatalogItemId { get; set; }         // service
    public DateTime AppointmentStart { get; set; }
    public int PlannedDurationMin { get; set; }
    public string? Notes { get; set; }
    public string? TableOrArea { get; set; }       // optional for café
}