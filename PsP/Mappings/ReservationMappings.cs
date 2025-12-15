using PsP.Contracts.Reservations;
using PsP.Models;

namespace PsP.Mappings;

public static class ReservationMappings
{
    public static ReservationSummaryResponse ToSummaryResponse(this Reservation r) => new()
    {
        ReservationId = r.ReservationId,
        BusinessId = r.BusinessId,
        EmployeeId = r.EmployeeId,
        CatalogItemId = r.CatalogItemId,
        AppointmentStart = r.AppointmentStart,
        PlannedDurationMin = r.PlannedDurationMin,
        Status = r.Status
    };

    public static ReservationDetailResponse ToDetailResponse(this Reservation r) => new()
    {
        ReservationId = r.ReservationId,
        BusinessId = r.BusinessId,
        EmployeeId = r.EmployeeId,
        CatalogItemId = r.CatalogItemId,
        AppointmentStart = r.AppointmentStart,
        PlannedDurationMin = r.PlannedDurationMin,
        Status = r.Status,
        BookedAt = r.BookedAt,
        Notes = r.Notes,
        TableOrArea = r.TableOrArea,
        OrderId = r.OrderId
    };

  
    public static Reservation ToNewEntity(
        this CreateReservationRequest req,
        int businessId,
        int assignedEmployeeId,
        int plannedDurationMin,
        DateTime bookedAtUtc)
        => new()
        {
            BusinessId = businessId,
            EmployeeId = assignedEmployeeId,
            CatalogItemId = req.CatalogItemId,
            BookedAt = bookedAtUtc,
            AppointmentStart = req.AppointmentStart,
            PlannedDurationMin = plannedDurationMin,
            Status = "Booked",
            Notes = req.Notes,
            TableOrArea = req.TableOrArea
        };
       

 
    public static void ApplyUpdate(
        this UpdateReservationRequest req,
        Reservation r,
        DateTime? newStart,
        int? plannedDurationMin)
    {
        if (newStart.HasValue) r.AppointmentStart = newStart.Value;

        if (req.EmployeeId.HasValue) r.EmployeeId = req.EmployeeId.Value;
        if (req.CatalogItemId.HasValue) r.CatalogItemId = req.CatalogItemId.Value;

        if (plannedDurationMin.HasValue) r.PlannedDurationMin = plannedDurationMin.Value;

        if (req.Notes is not null) r.Notes = req.Notes;
        if (req.TableOrArea is not null) r.TableOrArea = req.TableOrArea;
        if (!string.IsNullOrWhiteSpace(req.Status)) r.Status = req.Status;
    }
}
