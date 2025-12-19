namespace PsP.Contracts.Reservations;

public class CreateReservationRequest
{
    public int CatalogItemId { get; set; }
    public int? EmployeeId { get; set; }
    public DateTime AppointmentStart { get; set; }
  
    public string? Notes { get; set; }
    public string? TableOrArea { get; set; }      
}