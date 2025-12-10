using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Reservations;
using PsP.Models;
using PsP.Services.Implementations;

namespace TestProject1;

public class ReservationTests
{
    [Fact]
    public async Task Reservation_LinksOneToOne_WithOrder()
    {
        var (context, connection) = await TestHelpers.TestDbSqlite.NewAsync();
        await using var conn = connection; // keep SQLite in-memory connection alive
        await using var db = context;

        var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);
        var service = TestHelpers.SeedCatalogItem(db, biz.BusinessId, "Private Table", "Service");

        var res = new Reservation
        {
            BusinessId = biz.BusinessId,
            EmployeeId = emp.EmployeeId,
            CatalogItemId = service.CatalogItemId,
            BookedAt = DateTime.UtcNow,
            AppointmentStart = DateTime.UtcNow.AddHours(1),
            AppointmentEnd = DateTime.UtcNow.AddHours(2),
            PlannedDurationMin = 60,
            Status = "Booked",
            Notes = "Catering hall A"
        };
        db.Reservations.Add(res);
        await db.SaveChangesAsync();

        var order = new Order
        {
            BusinessId = biz.BusinessId,
            EmployeeId = emp.EmployeeId,
            ReservationId = res.ReservationId,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var loaded = await db.Orders
            .Include(o => o.Reservation)
            .FirstAsync(o => o.OrderId == order.OrderId);

        Assert.NotNull(loaded.Reservation);
        Assert.Equal(res.ReservationId, loaded.ReservationId);
    }

    [Fact]
    public async Task CreateAsync_Rejects_EndBeforeStart_OrDurationMismatch()
    {
        await using var db = TestHelpers.NewInMemoryContext();

        var biz = new Business { Name = "Catering Co", Address = "123 Food St", Phone = "1", Email = "catering@test.local", CountryCode = "LT", PriceIncludesTax = false };
        db.Businesses.Add(biz);
        await db.SaveChangesAsync();

        var owner = new Employee
        {
            BusinessId = biz.BusinessId,
            Name = "Chef Manager",
            Role = "Owner",
            Status = "Active",
            Email = "chef@catering.local",
            PasswordHash = "x"
        };
        db.Employees.Add(owner);

        var service = new CatalogItem
        {
            BusinessId = biz.BusinessId,
            Name = "Banquet Setup",
            Code = "CAT-SETUP",
            Type = "Service",
            BasePrice = 250,
            Status = "Active",
            DefaultDurationMin = 30,
            TaxClass = "Standard"
        };
        db.CatalogItems.Add(service);
        await db.SaveChangesAsync();

        var svc = new ReservationService(db);

        var start = DateTime.UtcNow;
        var request = new CreateReservationRequest
        {
            CatalogItemId = service.CatalogItemId,
            EmployeeId = owner.EmployeeId,
            AppointmentStart = start,
            AppointmentEnd = start, // invalid: end == start
            PlannedDurationMin = 30
        };

        var ex1 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(biz.BusinessId, owner.EmployeeId, request));
        Assert.Contains("appointment_end_before_start", ex1.Message);

        request.AppointmentEnd = start.AddMinutes(50); // mismatch vs duration 30
        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(biz.BusinessId, owner.EmployeeId, request));
        Assert.Contains("appointment_duration_mismatch", ex2.Message);
    }

    [Fact]
    public async Task UpdateAsync_RecomputesTriad_AndRejectsMismatch()
    {
        await using var db = TestHelpers.NewInMemoryContext();

        var biz = new Business { Name = "Catering Co", Address = "123 Food St", Phone = "1", Email = "catering@test.local", CountryCode = "LT", PriceIncludesTax = false };
        db.Businesses.Add(biz);
        await db.SaveChangesAsync();

        var owner = new Employee
        {
            BusinessId = biz.BusinessId,
            Name = "Chef Manager",
            Role = "Owner",
            Status = "Active",
            Email = "chef@catering.local",
            PasswordHash = "x"
        };
        db.Employees.Add(owner);

        var service = new CatalogItem
        {
            BusinessId = biz.BusinessId,
            Name = "Catering Service",
            Code = "CAT-SVC",
            Type = "Service",
            BasePrice = 400,
            Status = "Active",
            DefaultDurationMin = 30,
            TaxClass = "Standard"
        };
        db.CatalogItems.Add(service);
        await db.SaveChangesAsync();

        var svc = new ReservationService(db);

        var start = DateTime.UtcNow;
        var createReq = new CreateReservationRequest
        {
            CatalogItemId = service.CatalogItemId,
            EmployeeId = owner.EmployeeId,
            AppointmentStart = start,
            AppointmentEnd = start.AddMinutes(30),
            PlannedDurationMin = 30
        };

        var created = await svc.CreateAsync(biz.BusinessId, owner.EmployeeId, createReq);

        // Mismatched end vs duration
        var badUpdate = new UpdateReservationRequest
        {
            AppointmentEnd = start.AddMinutes(10), // does not match existing duration 30
            PlannedDurationMin = 30
        };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.UpdateAsync(biz.BusinessId, created.ReservationId, owner.EmployeeId, badUpdate));
        Assert.Contains("appointment_duration_mismatch", ex.Message);

        // Valid partial update: new duration recalculates end if not provided
        var goodUpdate = new UpdateReservationRequest
        {
            PlannedDurationMin = 45
        };
        var updated = await svc.UpdateAsync(biz.BusinessId, created.ReservationId, owner.EmployeeId, goodUpdate);
        Assert.Equal(45, updated.PlannedDurationMin);
        Assert.Equal(createReq.AppointmentStart.AddMinutes(45), updated.AppointmentEnd);
    }
}