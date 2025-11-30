using PsP.Services.Interfaces;

namespace PsP.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Orders;
using PsP.Data;
using PsP.Mappings;
using PsP.Models;

    public class OrdersService : IOrdersService
    {
        private readonly AppDbContext _db;

        public OrdersService(AppDbContext db) => _db = db;

        public async Task<OrderSummaryResponse> CreateOrderAsync(
            int businessId,
            CreateOrderRequest request,
            CancellationToken ct = default)
        {
            var businessExists = await _db.Businesses
                .AnyAsync(b => b.BusinessId == businessId, ct);
            if (!businessExists)
                throw new InvalidOperationException("Business not found.");

            // 2) Ensure an Employee exists (bootstrap if needed)
            var employeeId = await EnsureEmployeeAsync(businessId, request.EmployeeId, ct);

            int? requestedReservationId =
                (request.ReservationId.HasValue && request.ReservationId.Value > 0)
                    ? request.ReservationId
                    : null;
            // 3) Optional reservation sanity (only if provided)
            Reservation? reservation = null;
            if (request.ReservationId.HasValue)
            {
                reservation = await _db.Reservations
                    .Include(r => r.Order)
                    .FirstOrDefaultAsync(r =>
                        r.ReservationId == request.ReservationId.Value &&
                        r.BusinessId == businessId, ct);

                if (reservation is null)
                    throw new InvalidOperationException("Reservation not found for this business.");
                if (reservation.Order is not null)
                    throw new InvalidOperationException("Reservation is already linked to an order.");
            }

            // 4) Build entity (use your mapper), override EmployeeId with ensured one
            var order = request.ToNewEntity(businessId);
            order.EmployeeId = employeeId;
            if (reservation is not null) order.ReservationId = reservation.ReservationId;

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(ct);

            // Return detail with empty lines
            return order.ToSummaryResponse();
        }
        
        
        
        private async Task<int> EnsureEmployeeAsync(int businessId, int requestedEmployeeId, CancellationToken ct)
        {
            // If a valid employee id is provided and exists — use it.
            if (requestedEmployeeId > 0)
            {
                var exists = await _db.Employees
                    .AnyAsync(e => e.EmployeeId == requestedEmployeeId && e.BusinessId == businessId);
                if (exists) return requestedEmployeeId;
            }

            // Otherwise, try to find (or create) a single default employee for this business.
            var existingDefault = await _db.Employees
                .FirstOrDefaultAsync(e => e.BusinessId == businessId && e.Role == "Owner" && e.Status == "Active");

            if (existingDefault is not null) return existingDefault.EmployeeId;

            var bootstrap = new Employee
            {
                BusinessId = businessId,
                Name = "Default",
                Role = "Owner",
                Status = "Active"
            };
            _db.Employees.Add(bootstrap);
            await _db.SaveChangesAsync(ct);
            return bootstrap.EmployeeId;
        }
        
        
        
        
    }
    