namespace PsP.Contracts.Reservations;

public class ReservationDetailResponse : ReservationSummaryResponse
{
    public DateTime BookedAt { get; set; }
    public int PlannedDurationMin { get; set; }
    public string? Notes { get; set; }
    public string? TableOrArea { get; set; }
    public int? OrderId { get; set; } 
}