using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Orders;
using PsP.Data;
using PsP.Models;
using PsP.Services.Implementations;

namespace TestProject1;

public class TaxSnapshotTests
{
   
    private static (AppDbContext db, OrdersService svc, Business biz, Employee emp, CatalogItem item)
        Boot(decimal basePrice = 10.00m, bool priceIncludesTax = true, string taxClass = "Food")
    {
        var db = TestHelpers.NewContext();

     
        var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);
        biz.PriceIncludesTax = priceIncludesTax;
        db.Update(biz);

        
        var item = new CatalogItem
        {
            BusinessId = biz.BusinessId,
            Name = "Coffee",
            Code = "COF",
            Type = "Product",
            BasePrice = basePrice,
            Status = "Active",
            DefaultDurationMin = 0,
            TaxClass = taxClass
        };

        db.CatalogItems.Add(item);
        db.SaveChanges();

      
        db.StockItems.Add(new StockItem
        {
            CatalogItemId = item.CatalogItemId,
            Unit = "pcs",
            QtyOnHand = 999,
            AverageUnitCost = 1m
        });
        db.SaveChanges();

        var disc = new DiscountsService(db);
        var stocks = new StockMovementService(db);
        var svc = new OrdersService(db, disc, stocks);

        return (db, svc, biz, emp, item);
    }

  


    [Fact]
    public async Task AddLine_CapturesTaxClass_And_CurrentRate_From_TaxRules()
    {
        var (db, svc, biz, emp, item) = Boot();

        var now = DateTime.UtcNow;
        db.TaxRules.AddRange(
            new TaxRule { CountryCode = biz.CountryCode, TaxClass = item.TaxClass, RatePercent = 9.00m,  ValidFrom = now.AddYears(-2), ValidTo = now.AddYears(-1) },
            new TaxRule { CountryCode = biz.CountryCode, TaxClass = item.TaxClass, RatePercent = 21.00m, ValidFrom = now.AddDays(-1),  ValidTo = now.AddYears(1) }
        );
        await db.SaveChangesAsync();

        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        var line = await svc.AddLineAsync(
            biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m });

        var row = await db.OrderLines.AsNoTracking().FirstAsync(x => x.OrderLineId == line.OrderLineId);
        Assert.Equal(item.TaxClass, row.TaxClassSnapshot);
        Assert.Equal(21.00m, row.TaxRateSnapshotPct);
    }

    [Fact]
    public async Task AddLine_Uses_Business_PriceIncludesTax_Flag_But_Still_Snapshots_Rate()
    {
        var (db, svc, biz, emp, item) = Boot(basePrice: 12.34m, priceIncludesTax: false);

        var now = DateTime.UtcNow;
        db.TaxRules.Add(new TaxRule
        {
            CountryCode = biz.CountryCode,
            TaxClass = item.TaxClass,
            RatePercent = 15.00m,
            ValidFrom = now.AddDays(-1),
            ValidTo = now.AddYears(1)
        });
        await db.SaveChangesAsync();

        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        var line = await svc.AddLineAsync(
            biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 2m });

        var row = await db.OrderLines.AsNoTracking().FirstAsync(x => x.OrderLineId == line.OrderLineId);

        Assert.Equal("Food", row.TaxClassSnapshot);
        Assert.Equal(15.00m, row.TaxRateSnapshotPct);
        Assert.Equal(12.34m, row.UnitPriceSnapshot); 
    }

    [Fact]
    public async Task AddLine_Picks_Newest_Overlapping_Rule_By_ValidFrom()
    {
        var (db, conn) = await TestHelpers.TestDbSqlite.NewAsync();
        await using var _ = conn;
        await using var __ = db;

        var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);
        var item = TestHelpers.SeedCatalogItem(db, biz.BusinessId);
        TestHelpers.SeedStockItem(db, item.CatalogItemId);

        biz.CountryCode = "ZZ";
        item.TaxClass = "Food";
        db.Update(biz);
        db.Update(item);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;

        db.TaxRules.Add(new TaxRule {
            CountryCode = "ZZ", TaxClass = "Food",
            RatePercent = 15m, ValidFrom = now.AddDays(-10), ValidTo = now.AddDays(10)
        });
        db.TaxRules.Add(new TaxRule {
            CountryCode = "ZZ", TaxClass = "Food",
            RatePercent = 12m, ValidFrom = now.AddDays(-1), ValidTo = now.AddDays(10)
        });
        await db.SaveChangesAsync();

        var disc = new DiscountsService(db);
        var stocks = new StockMovementService(db);
        var svc = new OrdersService(db, disc, stocks);

        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        var line = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m });

        var row = await db.OrderLines.AsNoTracking().FirstAsync(x => x.OrderLineId == line.OrderLineId);
        Assert.Equal(12.00m, row.TaxRateSnapshotPct);
    }

    [Fact]
    public async Task AddLine_NoMatchingRule_Defaults_Rate_To_Zero()
    {
        var (db, svc, biz, emp, item) = Boot();

        biz.CountryCode = "ZZ";
        item.TaxClass = "NoTax-" + Guid.NewGuid().ToString("N")[..8];
        db.Update(biz);
        db.Update(item);
        await db.SaveChangesAsync();

        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        var line = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m });

        var row = await db.OrderLines.AsNoTracking().FirstAsync(x => x.OrderLineId == line.OrderLineId);
        Assert.Equal(0m, row.TaxRateSnapshotPct);
    }
}
