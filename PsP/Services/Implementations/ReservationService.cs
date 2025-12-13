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

    public ReservationService(AppDbContext db) => _db = db;

    // -----------------------------
    // Security / lookups
    // -----------------------------
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

    private async Task<Reservation> GetReservationEntityAsync(int businessId, int reservationId, CancellationToken ct)
    {
        return await _db.Reservations
            .FirstOrDefaultAsync(r => r.BusinessId == businessId && r.ReservationId == reservationId, ct)
            ?? throw new InvalidOperationException("reservation_not_found");
    }

    private async Task<CatalogItem> GetCatalogItemAsync(int businessId, int catalogItemId, CancellationToken ct)
    {
        return await _db.CatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(ci => ci.BusinessId == businessId && ci.CatalogItemId == catalogItemId, ct)
            ?? throw new InvalidOperationException("catalog_item_not_found");
    }

    private static void EnsureServiceCatalogItem(CatalogItem item)
    {
        if (!string.Equals(item.Type, "Service", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("catalog_item_not_service");

        if (!string.Equals(item.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("catalog_item_not_active");
    }

    private static int ResolveDurationFromCatalog(CatalogItem item)
    {
        // Your new rule: duration ALWAYS comes from catalog.
        if (item.DefaultDurationMin <= 0)
            throw new InvalidOperationException("duration_invalid");

        return item.DefaultDurationMin;
    }

    private static void EnsureStatusModifiable(string currentStatus)
    {
        // Only allow changing booked reservations (matches your old behavior).
        if (!string.Equals(currentStatus, "Booked", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("reservation_not_modifiable");
    }

    private static void EnsureStatusCancellable(string currentStatus)
    {
        if (!string.Equals(currentStatus, "Booked", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("reservation_not_cancellable");
    }

    private static void EnsureStartValid(DateTime start)
    {
        // Minimal "bulletproof" sanity check:
        // prevent default(DateTime) accidents (0001-01-01)
        if (start == default)
            throw new InvalidOperationException("appointment_start_invalid");
    }

    // -----------------------------
    // Public API
    // -----------------------------
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
            q = q.Where(r => r.EmployeeId == caller.EmployeeId);

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(r => r.Status == status);

        if (dateFrom.HasValue)
            q = q.Where(r => r.AppointmentStart >= dateFrom.Value);

        if (dateTo.HasValue)
            q = q.Where(r => r.AppointmentStart <= dateTo.Value);

        // These filters only make sense for managers/owners, but harmless either way
        if (employeeId.HasValue)
            q = q.Where(r => r.EmployeeId == employeeId.Value);

        if (catalogItemId.HasValue)
            q = q.Where(r => r.CatalogItemId == catalogItemId.Value);

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
        await EnsureCanManageAsync(businessId, callerEmployeeId, ct);

        // Assign employee: if your contract has EmployeeId optional, keep this;
        // if it's required, this still works.
        var assignedEmployeeId = request.EmployeeId ?? callerEmployeeId;

        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.BusinessId == businessId && e.EmployeeId == assignedEmployeeId, ct)
            ?? throw new InvalidOperationException("employee_not_found");

        if (!string.Equals(employee.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("employee_inactive");

        EnsureStartValid(request.AppointmentStart);

        var catalogItem = await GetCatalogItemAsync(businessId, request.CatalogItemId, ct);
        EnsureServiceCatalogItem(catalogItem);

        var duration = ResolveDurationFromCatalog(catalogItem);

        var entity = new Reservation
        {
            BusinessId = businessId,
            EmployeeId = assignedEmployeeId,
            CatalogItemId = request.CatalogItemId,
            BookedAt = DateTime.UtcNow,
            AppointmentStart = request.AppointmentStart,
            PlannedDurationMin = duration,
            Status = "Booked",
            Notes = request.Notes,
            TableOrArea = request.TableOrArea,
        };

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

        var reservation = await GetReservationEntityAsync(businessId, reservationId, ct);

        EnsureStatusModifiable(reservation.Status);

        // If employee changes, validate it exists + active
        if (request.EmployeeId.HasValue)
        {
            var emp = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.BusinessId == businessId && e.EmployeeId == request.EmployeeId.Value, ct)
                ?? throw new InvalidOperationException("employee_not_found");

            if (!string.Equals(emp.Status, "Active", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("employee_inactive");
        }

        // Determine the catalog item to use after update
        int effectiveCatalogItemId = request.CatalogItemId ?? reservation.CatalogItemId;

        var effectiveCatalog = await GetCatalogItemAsync(businessId, effectiveCatalogItemId, ct);
        EnsureServiceCatalogItem(effectiveCatalog);

        // Determine new start
        var newStart = request.AppointmentStart ?? reservation.AppointmentStart;
        EnsureStartValid(newStart);

        // IMPORTANT: duration is ALWAYS from catalog item (after update)
        var newDuration = ResolveDurationFromCatalog(effectiveCatalog);

        // Apply fields
        reservation.AppointmentStart = newStart;
        reservation.PlannedDurationMin = newDuration;

        if (request.EmployeeId.HasValue) reservation.EmployeeId = request.EmployeeId.Value;
        if (request.CatalogItemId.HasValue) reservation.CatalogItemId = request.CatalogItemId.Value;
        if (request.Notes is not null) reservation.Notes = request.Notes;
        if (request.TableOrArea is not null) reservation.TableOrArea = request.TableOrArea;
        if (!string.IsNullOrWhiteSpace(request.Status)) reservation.Status = request.Status;

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

        var reservation = await GetReservationEntityAsync(businessId, reservationId, ct);

        EnsureStatusCancellable(reservation.Status);

        reservation.Status = "Cancelled";

        await _db.SaveChangesAsync(ct);

        return reservation.ToDetailResponse();
    }
}
