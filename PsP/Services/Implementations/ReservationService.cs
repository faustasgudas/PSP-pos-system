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

    private async Task<Employee> GetEmployeeAsync(int businessId, int employeeId, CancellationToken ct)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.BusinessId == businessId && e.EmployeeId == employeeId, ct)
            ?? throw new InvalidOperationException("employee_not_found");

        if (!string.Equals(employee.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("employee_inactive");

        return employee;
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
        if (item.DefaultDurationMin <= 0)
            throw new InvalidOperationException("duration_invalid");

        return item.DefaultDurationMin;
    }

    private static void EnsureStartValid(DateTime start)
    {
        if (start == default)
            throw new InvalidOperationException("appointment_start_invalid");
    }

    private static void EnsureStatusModifiable(string currentStatus)
    {
        if (!string.Equals(currentStatus, "Booked", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("reservation_not_modifiable");
    }

    private static void EnsureStatusCancellable(string currentStatus)
    {
        if (!string.Equals(currentStatus, "Booked", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("reservation_not_cancellable");
    }

  
    private static void EnsureCanTouchReservation(Employee caller, Reservation r)
    {
        if (IsOwnerOrManager(caller)) return;

        if (r.EmployeeId != caller.EmployeeId)
            throw new InvalidOperationException("forbidden");
    }

  
    private static int ResolveAssignedEmployeeId(Employee caller, CreateReservationRequest req)
    {
        if (IsOwnerOrManager(caller))
            return req.EmployeeId ?? caller.EmployeeId;

      
        if (req.EmployeeId.HasValue && req.EmployeeId.Value != caller.EmployeeId)
            throw new InvalidOperationException("forbidden");

        return caller.EmployeeId;
    }

   
    private static void EnsureCanChangeEmployee(Employee caller, int currentEmployeeId, int newEmployeeId)
    {
        if (IsOwnerOrManager(caller)) return;

        if (newEmployeeId != currentEmployeeId)
            throw new InvalidOperationException("forbidden");
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

        var q = _db.Reservations.AsNoTracking().Where(r => r.BusinessId == businessId);

        
        if (!IsOwnerOrManager(caller))
            q = q.Where(r => r.EmployeeId == caller.EmployeeId);

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(r => r.Status == status);

        if (dateFrom.HasValue)
            q = q.Where(r => r.AppointmentStart >= dateFrom.Value);

        if (dateTo.HasValue)
            q = q.Where(r => r.AppointmentStart <= dateTo.Value);

        
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
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);

        EnsureStartValid(request.AppointmentStart);

        var assignedEmployeeId = ResolveAssignedEmployeeId(caller, request);
        _ = await GetEmployeeAsync(businessId, assignedEmployeeId, ct);

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
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);

        var reservation = await GetReservationEntityAsync(businessId, reservationId, ct);
        EnsureCanTouchReservation(caller, reservation);
        EnsureStatusModifiable(reservation.Status);

       
        var effectiveEmployeeId = request.EmployeeId ?? reservation.EmployeeId;
        EnsureCanChangeEmployee(caller, reservation.EmployeeId, effectiveEmployeeId);

        
        if (effectiveEmployeeId != reservation.EmployeeId)
            _ = await GetEmployeeAsync(businessId, effectiveEmployeeId, ct);

        
        var effectiveCatalogItemId = request.CatalogItemId ?? reservation.CatalogItemId;
        var effectiveCatalog = await GetCatalogItemAsync(businessId, effectiveCatalogItemId, ct);
        EnsureServiceCatalogItem(effectiveCatalog);

    
        var newStart = request.AppointmentStart ?? reservation.AppointmentStart;
        EnsureStartValid(newStart);

        
        var newDuration = ResolveDurationFromCatalog(effectiveCatalog);

        
        reservation.AppointmentStart = newStart;
        reservation.PlannedDurationMin = newDuration;
        reservation.EmployeeId = effectiveEmployeeId;
        reservation.CatalogItemId = effectiveCatalogItemId;

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
        var caller = await GetCallerAsync(businessId, callerEmployeeId, ct);

        var reservation = await GetReservationEntityAsync(businessId, reservationId, ct);
        EnsureCanTouchReservation(caller, reservation);
        EnsureStatusCancellable(reservation.Status);

        reservation.Status = "Cancelled";
        await _db.SaveChangesAsync(ct);

        return reservation.ToDetailResponse();
    }
}
