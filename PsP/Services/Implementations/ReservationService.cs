using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Reservations;
using PsP.Data;
using PsP.Mappings;
using PsP.Models;
using PsP.Services.Interfaces;

namespace PsP.Services.Implementations;

public class ReservationService : IReservationService
{
    private readonly AppDbContext _db;

    public ReservationService(AppDbContext db)
    {
        _db = db;
    }

    private async Task<Employee> GetCallerAsync(int businessId, int callerEmployeeId, CancellationToken ct)
    {
        var caller = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.BusinessId == businessId && e.EmployeeId == callerEmployeeId, ct)
            ?? throw new InvalidOperationException("caller_not_found");

        if (!string.Equals(caller.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("caller_inactive");

        return caller;
    }

    private static bool IsOwnerOrManager(Employee e) =>
        string.Equals(e.Role, "Owner", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(e.Role, "Manager", StringComparison.OrdinalIgnoreCase);

    private async Task EnsureCanManageAsync(int businessId, int callerEmployeeId, CancellationToken ct)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);
        if (!IsOwnerOrManager(caller))
            throw new InvalidOperationException("forbidden");
    }

    private static void EnsureServiceCatalogItem(CatalogItem item)
    {
        if (!string.Equals(item.Type, "Service", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("catalog_item_not_service");
        if (!string.Equals(item.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("catalog_item_not_active");
    }

    private async Task<CatalogItem> GetCatalogItemAsync(int businessId, int catalogItemId, CancellationToken ct)
    {
        return await _db.CatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(ci => ci.BusinessId == businessId && ci.CatalogItemId == catalogItemId, ct)
            ?? throw new InvalidOperationException("catalog_item_not_found");
    }

    private static void EnsureStatusCanChange(string currentStatus)
    {
        if (!string.Equals(currentStatus, "Booked", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("reservation_not_modifiable");
    }

    private static void EnsureStatusCanCancel(string currentStatus)
    {
        if (!string.Equals(currentStatus, "Booked", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("reservation_not_cancellable");
    }

    private static void EnsureValidTimeRange(DateTime start, DateTime end, int durationMinutes)
    {
        if (end <= start)
            throw new InvalidOperationException("appointment_end_before_start");

        var computed = (int)Math.Round((end - start).TotalMinutes, MidpointRounding.AwayFromZero);
        if (computed != durationMinutes)
            throw new InvalidOperationException("appointment_duration_mismatch");
    }

    public async Task<IEnumerable<ReservationSummaryResponse>> ListAsync(
        int businessId,
        int callerEmployeeId,
        string? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? employeeId,
        int? catalogItemId,
        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);

        var q = _db.Reservations
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId);

        if (!IsOwnerOrManager(caller))
        {
            // staff: only own reservations
            q = q.Where(r => r.EmployeeId == caller.EmployeeId);
        }

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(r => r.Status == status);
        if (dateFrom.HasValue) q = q.Where(r => r.AppointmentStart >= dateFrom.Value);
        if (dateTo.HasValue) q = q.Where(r => r.AppointmentStart <= dateTo.Value);
        if (employeeId.HasValue) q = q.Where(r => r.EmployeeId == employeeId.Value);
        if (catalogItemId.HasValue) q = q.Where(r => r.CatalogItemId == catalogItemId.Value);

        var list = await q
            .OrderBy(r => r.AppointmentStart)
            .ThenBy(r => r.ReservationId)
            .ToListAsync(ct);

        return list.Select(r => r.ToSummaryResponse());
    }

    public async Task<ReservationDetailResponse> GetAsync(
        int businessId,
        int reservationId,
        int callerEmployeeId,
        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);

        var reservation = await _db.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BusinessId == businessId && r.ReservationId == reservationId, ct)
            ?? throw new InvalidOperationException("reservation_not_found");

        if (!IsOwnerOrManager(caller) && reservation.EmployeeId != caller.EmployeeId)
            throw new InvalidOperationException("forbidden");

        return reservation.ToDetailResponse();
    }

    public async Task<ReservationDetailResponse> CreateAsync(
        int businessId,
        int callerEmployeeId,
        CreateReservationRequest request,
        CancellationToken ct = default)
    {
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);
        if (!IsOwnerOrManager(caller))
            throw new InvalidOperationException("forbidden");

        var assignedEmployeeId = request.EmployeeId ?? callerEmployeeId;
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == assignedEmployeeId && e.BusinessId == businessId, ct)
            ?? throw new InvalidOperationException("employee_not_found");
        if (!string.Equals(employee.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("employee_inactive");

        var catalogItem = await GetCatalogItemAsync(businessId, request.CatalogItemId, ct);
        EnsureServiceCatalogItem(catalogItem);

        if (request.PlannedDurationMin <= 0)
            throw new InvalidOperationException("duration_invalid");

        EnsureValidTimeRange(request.AppointmentStart, request.AppointmentEnd, request.PlannedDurationMin);

        var entity = request.ToNewEntity(businessId, assignedEmployeeId, DateTime.UtcNow);

        _db.Reservations.Add(entity);
        await _db.SaveChangesAsync(ct);

        return entity.ToDetailResponse();
    }

    public async Task<ReservationDetailResponse> UpdateAsync(
        int businessId,
        int reservationId,
        int callerEmployeeId,
        UpdateReservationRequest request,
        CancellationToken ct = default)
    {
        await EnsureCanManageAsync(businessId, callerEmployeeId, ct);

        var reservation = await _db.Reservations
            .FirstOrDefaultAsync(r => r.BusinessId == businessId && r.ReservationId == reservationId, ct)
            ?? throw new InvalidOperationException("reservation_not_found");

        EnsureStatusCanChange(reservation.Status);

        if (request.PlannedDurationMin.HasValue && request.PlannedDurationMin <= 0)
            throw new InvalidOperationException("duration_invalid");

        if (request.EmployeeId.HasValue)
        {
            var emp = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId.Value && e.BusinessId == businessId, ct)
                ?? throw new InvalidOperationException("employee_not_found");
            if (!string.Equals(emp.Status, "Active", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("employee_inactive");
        }

        if (request.CatalogItemId.HasValue)
        {
            var catalogItem = await GetCatalogItemAsync(businessId, request.CatalogItemId.Value, ct);
            EnsureServiceCatalogItem(catalogItem);
        }

        // derive consistent start/end/duration trio
        var newStart = request.AppointmentStart ?? reservation.AppointmentStart;
        var newDuration = request.PlannedDurationMin ?? reservation.PlannedDurationMin;

        DateTime newEnd;
        if (request.AppointmentEnd.HasValue)
        {
            newEnd = request.AppointmentEnd.Value;
            if (request.PlannedDurationMin.HasValue)
            {
                // ensure provided end matches provided duration
                EnsureValidTimeRange(newStart, newEnd, newDuration);
            }
            else
            {
                // recompute duration from provided end
                newDuration = (int)Math.Round((newEnd - newStart).TotalMinutes, MidpointRounding.AwayFromZero);
            }
        }
        else
        {
            // recompute end from start + duration if end not provided
            newEnd = newStart.AddMinutes(newDuration);
        }

        EnsureValidTimeRange(newStart, newEnd, newDuration);

        request.ApplyUpdate(reservation, newStart, newEnd, newDuration);

        await _db.SaveChangesAsync(ct);
        return reservation.ToDetailResponse();
    }

    public async Task<ReservationDetailResponse> CancelAsync(
        int businessId,
        int reservationId,
        int callerEmployeeId,
        CancellationToken ct = default)
    {
        await EnsureCanManageAsync(businessId, callerEmployeeId, ct);

        var reservation = await _db.Reservations
            .FirstOrDefaultAsync(r => r.BusinessId == businessId && r.ReservationId == reservationId, ct)
            ?? throw new InvalidOperationException("reservation_not_found");

        EnsureStatusCanCancel(reservation.Status);
        reservation.Status = "Cancelled";

        await _db.SaveChangesAsync(ct);
        return reservation.ToDetailResponse();
    }

}

