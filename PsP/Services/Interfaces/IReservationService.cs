using PsP.Contracts.Reservations;

namespace PsP.Services.Interfaces;

public interface IReservationService
{
    Task<IEnumerable<ReservationSummaryResponse>> ListAsync(
        int businessId,
        int callerEmployeeId,
        string? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? employeeId,
        int? catalogItemId,
        CancellationToken ct = default);

    Task<ReservationDetailResponse> GetAsync(
        int businessId,
        int reservationId,
        int callerEmployeeId,
        CancellationToken ct = default);

    Task<ReservationDetailResponse> CreateAsync(
        int businessId,
        int callerEmployeeId,
        CreateReservationRequest request,
        CancellationToken ct = default);

    Task<ReservationDetailResponse> UpdateAsync(
        int businessId,
        int reservationId,
        int callerEmployeeId,
        UpdateReservationRequest request,
        CancellationToken ct = default);

    Task<ReservationDetailResponse> CancelAsync(
        int businessId,
        int reservationId,
        int callerEmployeeId,
        CancellationToken ct = default);
}

