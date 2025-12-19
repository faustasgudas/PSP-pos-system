namespace PsP.Contracts.Reservations;

public class UpdateReservationRequest
{
    public int? EmployeeId { get; set; }
    public int? CatalogItemId { get; set; }
    public DateTime? AppointmentStart { get; set; }
   
    
    public string? Notes { get; set; }
    public string? TableOrArea { get; set; }
    public string? Status { get; set; }    
}