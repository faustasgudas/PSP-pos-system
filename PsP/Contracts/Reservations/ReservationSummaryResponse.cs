namespace PsP.Contracts.Reservations;

public class ReservationSummaryResponse
{
    public int ReservationId { get; set; }
    public int BusinessId { get; set; }
    public int EmployeeId { get; set; }
    public int CatalogItemId { get; set; }
    public DateTime AppointmentStart { get; set; }
    public DateTime AppointmentEnd { get; set; }
    public string Status { get; set; } = null!;
}
