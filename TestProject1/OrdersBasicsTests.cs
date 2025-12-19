using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsP.Models;
using TestProject1;   // TestHelpers
using Xunit;

namespace PsP.Tests;

public class OrdersBasicsTests
{
    [Fact]
    public async Task CreateOrder_WithTwoLines_PersistsSnapshots_AndNavs()
    {
        await using var db = TestHelpers.NewInMemoryContext();  
        var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);
        var item1 = TestHelpers.SeedCatalogItem(db, biz.BusinessId, "Espresso", "Food");
        var item2 = TestHelpers.SeedCatalogItem(db, biz.BusinessId, "Haircut", "Service");

        var order = new Order
        {
            BusinessId = biz.BusinessId,
            EmployeeId = emp.EmployeeId,
            Status = "Open",
            CreatedAt = DateTime.UtcNow,
            TableOrArea = "A1"
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var line1 = new OrderLine
        {
            BusinessId = biz.BusinessId,
            OrderId = order.OrderId,
            CatalogItemId = item1.CatalogItemId,
            Qty = 2m,
            ItemNameSnapshot = item1.Name,
            UnitPriceSnapshot = item1.BasePrice,
            TaxClassSnapshot = item1.TaxClass,
            TaxRateSnapshotPct = 21.00m,
            PerformedAt = DateTime.UtcNow,
            PerformedByEmployeeId = emp.EmployeeId
        };
        var line2 = new OrderLine
        {
            BusinessId = biz.BusinessId,
            OrderId = order.OrderId,
            CatalogItemId = item2.CatalogItemId,
            Qty = 1m,
            ItemNameSnapshot = item2.Name,
            UnitPriceSnapshot = item2.BasePrice,
            TaxClassSnapshot = item2.TaxClass,
            TaxRateSnapshotPct = 0.00m,
            PerformedAt = DateTime.UtcNow,
            PerformedByEmployeeId = emp.EmployeeId
        };

        db.OrderLines.AddRange(line1, line2);
        await db.SaveChangesAsync();

        var loaded = await db.Orders
            .Include(o => o.Lines)
            .FirstAsync(o => o.OrderId == order.OrderId);

        Assert.Equal(2, loaded.Lines.Count);
        Assert.All(loaded.Lines, l =>
        {
            Assert.False(string.IsNullOrWhiteSpace(l.ItemNameSnapshot));
            Assert.True(l.UnitPriceSnapshot >= 0);
            Assert.NotEqual(default, l.PerformedAt);
        });
    }

    [Fact]
    public async Task DeleteOrder_ShouldFail_WhenOrderLinesExist()
    {
        await using var db = TestHelpers.NewInMemoryContext();

        
        var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);
        var item = TestHelpers.SeedCatalogItem(db, biz.BusinessId);

        var order = new Order
        {
            BusinessId = biz.BusinessId,
            EmployeeId = emp.EmployeeId,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        db.OrderLines.Add(new OrderLine
        {
            BusinessId = biz.BusinessId,
            OrderId = order.OrderId,
            CatalogItemId = item.CatalogItemId,
            Qty = 1,
            ItemNameSnapshot = item.Name,
            UnitPriceSnapshot = item.BasePrice,
            TaxClassSnapshot = item.TaxClass,
            TaxRateSnapshotPct = 21m,
            PerformedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

       
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            db.Orders.Remove(order);
            await db.SaveChangesAsync();
        });

     
        var count = await db.OrderLines
            .Where(l => l.OrderId == order.OrderId)
            .CountAsync();

        Assert.Equal(1, count);
    }

}
