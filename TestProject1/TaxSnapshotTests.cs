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

            // business + employee
            var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);
            biz.PriceIncludesTax = priceIncludesTax;
            db.Update(biz);

            // catalog item
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

            // minimal order/discounts services
            var disc = new DiscountsService(db);
            var svc  = new OrdersService(db, disc);

            db.SaveChanges();
            return (db, svc, biz, emp, item);
        }

        [Fact]
        public async Task AddLine_CapturesTaxClass_And_CurrentRate_From_TaxRules()
        {
            var (db, svc, biz, emp, item) = Boot();

            // add 2 rules: only the current window must be chosen
            var now = DateTime.UtcNow;
            db.TaxRules.AddRange(
                new TaxRule { CountryCode = biz.CountryCode, TaxClass = item.TaxClass, RatePercent = 9.00m,  ValidFrom = now.AddYears(-2), ValidTo = now.AddYears(-1) },
                new TaxRule { CountryCode = biz.CountryCode, TaxClass = item.TaxClass, RatePercent = 21.00m, ValidFrom = now.AddDays(-1),  ValidTo = now.AddYears( 1) }
            );
            await db.SaveChangesAsync();

            var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,new CreateOrderRequest { EmployeeId = emp.EmployeeId });

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

            // current applicable rule
            var now = DateTime.UtcNow;
            db.TaxRules.Add(new TaxRule
            {
                CountryCode = biz.CountryCode,
                TaxClass    = item.TaxClass,
                RatePercent = 15.00m,
                ValidFrom   = now.AddDays(-1),
                ValidTo     = now.AddYears(1)
            });
            await db.SaveChangesAsync();

            var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,new CreateOrderRequest { EmployeeId = emp.EmployeeId });

            var line = await svc.AddLineAsync(
                biz.BusinessId, order.OrderId, emp.EmployeeId,
                new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 2m });

            var row = await db.OrderLines.AsNoTracking().FirstAsync(x => x.OrderLineId == line.OrderLineId);

            // We don’t assert totals here (not stored), but we do verify the snapshots used for tax math downstream.
            Assert.Equal("Food", row.TaxClassSnapshot);
            Assert.Equal(15.00m, row.TaxRateSnapshotPct);
            Assert.Equal(12.34m, row.UnitPriceSnapshot); // snapshot should always reflect catalog price at time of sale
        }

        [Fact]
        public async Task AddLine_Picks_Newest_Overlapping_Rule_By_ValidFrom()
        {
            var (db, conn) = await TestHelpers.TestDbSqlite.NewAsync();
            await using var _ = conn;
            await using var __ = db;

            var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);
            var item = TestHelpers.SeedCatalogItem(db, biz.BusinessId);

            // Make sure your rule filter will match these:
            biz.CountryCode = "ZZ";
            item.TaxClass   = "Food";
            db.Update(biz); db.Update(item);
            await db.SaveChangesAsync();

            var now = DateTime.UtcNow;

            // older 15%
            db.TaxRules.Add(new TaxRule {
                CountryCode = "ZZ", TaxClass = "Food",
                RatePercent = 15m, ValidFrom = now.AddDays(-10), ValidTo = now.AddDays(10)
            });
            // newer 12%
            db.TaxRules.Add(new TaxRule {
                CountryCode = "ZZ", TaxClass = "Food",
                RatePercent = 12m, ValidFrom = now.AddDays(-1), ValidTo = now.AddDays(10)
            });
            await db.SaveChangesAsync();

            var disc = new DiscountsService(db);
            var svc = new OrdersService(db, disc); // your factory

            var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,new CreateOrderRequest { EmployeeId = emp.EmployeeId });
            var line  = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
                new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m });

            var row = await db.OrderLines.AsNoTracking().FirstAsync(x => x.OrderLineId == line.OrderLineId);
            Assert.Equal(12.00m, row.TaxRateSnapshotPct);
        }

        [Fact]
        public async Task AddLine_NoMatchingRule_Defaults_Rate_To_Zero()
        {
            var (db, svc, biz, emp, item) = Boot();
        
            // Pick values no TaxRule will match
            biz.CountryCode = "ZZ"; // still fine, length 2
            item.TaxClass = "NoTax-" + Guid.NewGuid().ToString("N").Substring(0, 8); // <= 32
            db.Update(biz);
            db.Update(item);
            await db.SaveChangesAsync();
        
            // Do NOT insert any TaxRule
            var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,new CreateOrderRequest { EmployeeId = emp.EmployeeId });
            var line  = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
                new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m });
        
            var row = await db.OrderLines.AsNoTracking().FirstAsync(x => x.OrderLineId == line.OrderLineId);
            Assert.Equal(0m, row.TaxRateSnapshotPct);
        }
    
}