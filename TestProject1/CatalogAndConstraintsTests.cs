using Microsoft.EntityFrameworkCore;
using PsP.Models;
using TestProject1;

namespace PsP.Tests;

public class CatalogAndConstraintsTests
{
    [Fact]
    public async Task DeleteCatalogItem_IsRestricted_WhenReferencedByOrderLine()
    {
        await using var db = TestHelpers.NewContext();
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
        
        await using var db2 = TestHelpers.NewContext();
        var item2 = await db2.CatalogItems.FindAsync(item.CatalogItemId);
        db2.CatalogItems.Remove(item2!);

        await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());
    }

    [Fact]
    public async Task CatalogCode_IsUniqueWithinBusiness()
    {
        await using var db = TestHelpers.NewContext();
        var (biz, _) = TestHelpers.SeedBusinessAndEmployee(db);

        var code = "DUP-" + Guid.NewGuid().ToString("N")[..5];

        db.CatalogItems.Add(new CatalogItem
        {
            BusinessId = biz.BusinessId,
            Name = "Item A",
            Code = code,
            Type = "Product",
            BasePrice = 1.00m,
            Status = "Active",
            DefaultDurationMin = 0,
            TaxClass = "Food"
        });
        await db.SaveChangesAsync();

        db.CatalogItems.Add(new CatalogItem
        {
            BusinessId = biz.BusinessId,
            Name = "Item B",
            Code = code,
            Type = "Product",
            BasePrice = 2.00m,
            Status = "Active",
            DefaultDurationMin = 0,
            TaxClass = "Food"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DiscountEligibility_CompositeKey_PreventsDuplicates()
    {
        await using var db = TestHelpers.NewContext();
        var (biz, _) = TestHelpers.SeedBusinessAndEmployee(db);
        var item = TestHelpers.SeedCatalogItem(db, biz.BusinessId);

        var discount = new Discount
        {
            BusinessId = biz.BusinessId,
            Code = "ITEM10",
            Type = "Percent",
            Scope = "Line",
            Value = 10m,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            EndsAt = DateTime.UtcNow.AddDays(10),
            Status = "Active"
        };
        db.Discounts.Add(discount);
        await db.SaveChangesAsync();

        
        db.DiscountEligibilities.Add(new DiscountEligibility
        {
            DiscountId = discount.DiscountId,
            CatalogItemId = item.CatalogItemId
        });
        await db.SaveChangesAsync();

      
        await using var db2 = TestHelpers.NewContext();
        db2.DiscountEligibilities.Add(new DiscountEligibility
        {
            DiscountId = discount.DiscountId,
            CatalogItemId = item.CatalogItemId
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());
    }
}
