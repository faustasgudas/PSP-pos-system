using Microsoft.EntityFrameworkCore;
using PsP.Models;
using TestProject1;

namespace PsP.Tests;

public class ReservationTests
{
    [Fact]
    public async Task Reservation_LinksOneToOne_WithOrder()
    {
        await using var db = TestHelpers.NewContext();
        var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);
        var service = TestHelpers.SeedCatalogItem(db, biz.BusinessId, "Haircut", "Service");

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
            Notes = "VIP"
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
}