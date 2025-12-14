using Microsoft.EntityFrameworkCore;
using PsP.Data;
using PsP.Models;

namespace TestProject1;

public static class TestDb
{
    public static AppDbContext NewInMemory()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new AppDbContext(opts);
    }
}

public static class Seed
{
    public static (Business biz, Employee emp) BizAndStaff(AppDbContext db, string country = "LT")
    {
        var biz = new Business
        {
            Name = "Biz",
            Address = "Addr",
            Phone = "+370000000",
            Email = "a@b.c",
            BusinessType = "Catering",
            CountryCode = country,
            PriceIncludesTax = false
        };
        db.Businesses.Add(biz);
        db.SaveChanges();

        var emp = new Employee { BusinessId = biz.BusinessId, Name = "E1", Role = "Staff", Status = "Active",Email = "a@b.c",PasswordHash = "whatever" };
        db.Employees.Add(emp);
        db.SaveChanges();

        return (biz, emp);
    }

    public static Employee AddEmployee(AppDbContext db, int businessId, string role = "Staff", string name = "E2")
    {
        var e = new Employee { BusinessId = businessId, Name = name, Role = role, Status = "Active", Email = "a@b.c",PasswordHash = "whatever"};
        db.Employees.Add(e);
        db.SaveChanges();
        return e;
    }

    public static CatalogItem Item(AppDbContext db, int bizId, string taxClass = "Food", decimal price = 10m)
    {
        var item = new CatalogItem
        {
            BusinessId = bizId,
            Name = "Coffee",
            Code = "SKU-1",
            Type = "Product",
            BasePrice = price,
            Status = "Active",
            DefaultDurationMin = 0,
            TaxClass = taxClass
        };
        db.CatalogItems.Add(item);
        db.SaveChanges();

        // NEW: automatically create stock if item is a product
        if (string.Equals(item.Type, "Product", StringComparison.OrdinalIgnoreCase))
        {
            db.StockItems.Add(new StockItem
            {
                CatalogItemId = item.CatalogItemId,
                Unit = "pcs",
                QtyOnHand = 999,
                AverageUnitCost = 1m
            });
            db.SaveChanges();
        }

        return item;
    }
    
    public static StockItem SeedStockItem(AppDbContext db, int catalogItemId, decimal qty = 100)
    {
        var stock = new StockItem
        {
            CatalogItemId = catalogItemId,
            Unit = "pcs",
            QtyOnHand = qty,
            AverageUnitCost = 1m
        };

        db.StockItems.Add(stock);
        db.SaveChanges();
        return stock;
    }

    public static void Tax(AppDbContext db, string country, string taxClass, decimal rate, DateTime? from = null, DateTime? to = null)
    {
        db.TaxRules.Add(new TaxRule
        {
            CountryCode = country,
            TaxClass = taxClass,
            RatePercent = rate,
            ValidFrom = from ?? DateTime.UtcNow.AddDays(-1),
            ValidTo   = to   ?? DateTime.UtcNow.AddDays( 1)
        });
        db.SaveChanges();
    }

    public static Discount OrderDiscount(AppDbContext db, int bizId, decimal val)
    {
        var d = new Discount
        {
            BusinessId = bizId, Code = "ORD10", Type = "Percent", Scope = "Order", Value = val,
            StartsAt = DateTime.UtcNow.AddDays(-1), EndsAt = DateTime.UtcNow.AddDays(10), Status = "Active"
        };
        db.Discounts.Add(d);
        db.SaveChanges();
        return d;
    }

    public static Discount LineDiscountForItem(AppDbContext db, int bizId, int catalogItemId, decimal val)
    {
        var d = new Discount
        {
            BusinessId = bizId, Code = "LINE10", Type = "Percent", Scope = "Line", Value = val,
            StartsAt = DateTime.UtcNow.AddDays(-1), EndsAt = DateTime.UtcNow.AddDays(10), Status = "Active"
        };
        db.Discounts.Add(d);
        db.SaveChanges();

        db.DiscountEligibilities.Add(new DiscountEligibility
        {
            DiscountId = d.DiscountId, CatalogItemId = catalogItemId
        });
        db.SaveChanges();

        return d;
    }

    public static Reservation Reservation(AppDbContext db, int bizId, int empId, int itemId)
    {
        var r = new Reservation
        {
            BusinessId = bizId, EmployeeId = empId, CatalogItemId = itemId,
            BookedAt = DateTime.UtcNow,
            AppointmentStart = DateTime.UtcNow.AddHours(1),
            PlannedDurationMin = 60, Status = "Booked"
        };
        db.Reservations.Add(r);
        db.SaveChanges();
        return r;
    }
}