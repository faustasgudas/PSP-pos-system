using PsP.Contracts.Reservations;
using PsP.Models;

namespace PsP.Mappings;

public static class ReservationMappings
{
   public static ReservationSummaryResponse ToSummaryResponse(this Reservation r) => new()
    {
        ReservationId    = r.ReservationId,
        BusinessId       = r.BusinessId,
        EmployeeId       = r.EmployeeId,
        CatalogItemId    = r.CatalogItemId,
        AppointmentStart = r.AppointmentStart,
        AppointmentEnd   = r.AppointmentEnd,
        Status           = r.Status
    };

    public static ReservationDetailResponse ToDetailResponse(this Reservation r) => new()
    {
        ReservationId      = r.ReservationId,
        BusinessId         = r.BusinessId,
        EmployeeId         = r.EmployeeId,
        CatalogItemId      = r.CatalogItemId,
        AppointmentStart   = r.AppointmentStart,
        AppointmentEnd     = r.AppointmentEnd,
        Status             = r.Status,
        BookedAt           = r.BookedAt,
        PlannedDurationMin = r.PlannedDurationMin,
        Notes              = r.Notes,
        TableOrArea        = r.TableOrArea,
        
    };

    // Request -> Entity
    public static Reservation ToNewEntity(this CreateReservationRequest req, int businessId, DateTime? nowUtc = null)
    {
        if (req.EmployeeId <= 0) throw new ArgumentException("EmployeeId required");
        if (req.CatalogItemId <= 0) throw new ArgumentException("CatalogItemId required");
        if (req.PlannedDurationMin <= 0) throw new ArgumentException("PlannedDurationMin must be > 0");

        var start = req.AppointmentStart;
        var end = start.AddMinutes(req.PlannedDurationMin);

        return new Reservation
        {
            BusinessId         = businessId,
            EmployeeId         = req.EmployeeId,
            CatalogItemId      = req.CatalogItemId,
            BookedAt           = nowUtc ?? DateTime.UtcNow,
            AppointmentStart   = start,
            AppointmentEnd     = end,
            PlannedDurationMin = req.PlannedDurationMin,
            Status             = "Booked",
            Notes              = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim(),
            TableOrArea        = string.IsNullOrWhiteSpace(req.TableOrArea) ? null : req.TableOrArea.Trim()
        };
    }

    // Apply partial update
    public static void ApplyUpdate(this UpdateReservationRequest req, Reservation r)
    {
        var start = r.AppointmentStart;
        var duration = r.PlannedDurationMin;

        if (req.EmployeeId.HasValue)     r.EmployeeId = req.EmployeeId.Value;
        if (req.CatalogItemId.HasValue)  r.CatalogItemId = req.CatalogItemId.Value;

        if (req.AppointmentStart.HasValue) start = req.AppointmentStart.Value;
        if (req.PlannedDurationMin.HasValue && req.PlannedDurationMin.Value > 0)
            duration = req.PlannedDurationMin.Value;

        // recompute end if any time component changed
        if (req.AppointmentStart.HasValue || req.PlannedDurationMin.HasValue)
        {
            r.AppointmentStart   = start;
            r.PlannedDurationMin = duration;
            r.AppointmentEnd     = start.AddMinutes(duration);
        }

        if (req.Notes is not null)      r.Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();
        if (req.TableOrArea is not null)r.TableOrArea = string.IsNullOrWhiteSpace(req.TableOrArea) ? null : req.TableOrArea.Trim();

        if (!string.IsNullOrWhiteSpace(req.Status))
            r.Status = NormalizeReservationStatus(req.Status!);
    }

    private static string NormalizeReservationStatus(string status)
    {
        var s = status.Trim();
        return s.Equals("booked",    StringComparison.OrdinalIgnoreCase) ? "Booked"    :
               s.Equals("cancelled", StringComparison.OrdinalIgnoreCase) ? "Cancelled" :
               s.Equals("completed", StringComparison.OrdinalIgnoreCase) ? "Completed" :
               s;
    }  
}