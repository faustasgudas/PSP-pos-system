using System;
using Microsoft.EntityFrameworkCore;
using PsP.Data;
using PsP.Models;

namespace TestProject1;

public static class TestHelpers
{
    /// <summary>
    /// Tikra PostgreSQL DB – integration testams.
    /// </summary>
    public static AppDbContext NewContext()
    {
        // Same connection string as your appsettings (include error detail for easier debugging)
        var cs = Environment.GetEnvironmentVariable("PSP_TEST_CS")
                 ?? "Host=localhost;Port=5432;Database=pspdb;Username=postgres;Password=postgres;Include Error Detail=true";

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs)
            .EnableSensitiveDataLogging()
            .Options;

        return new AppDbContext(opts);
    }

    /// <summary>
    /// InMemory DB – greitiems unit testams (be tikros DB).
    /// </summary>
    public static AppDbContext NewInMemoryContext()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(opts);
    }

    public static (Business biz, Employee emp) SeedBusinessAndEmployee(AppDbContext db)
    {
        var biz = new Business
        {
            Name = "Biz " + Guid.NewGuid().ToString("N")[..6],
            Address = "Any St. 1",
            Phone = "+3700000000",
            Email = "biz@test.local",
            CountryCode = "LT",
            PriceIncludesTax = false
        };
        db.Businesses.Add(biz);
        db.SaveChanges();

        var emp = new Employee
        {
            BusinessId = biz.BusinessId,
            Name = "Alice",
            Role = "Staff",
            Status = "Active"
        };
        db.Employees.Add(emp);
        db.SaveChanges();

        return (biz, emp);
    }

    public static CatalogItem SeedCatalogItem(AppDbContext db, int businessId, string name = "Coffee", string taxClass = "Food")
    {
        var item = new CatalogItem
        {
            BusinessId = businessId,
            Name = name,
            Code = "SKU-" + Guid.NewGuid().ToString("N")[..6],
            Type = "Product",
            BasePrice = 2.35m,
            Status = "Active",
            DefaultDurationMin = 0,
            TaxClass = taxClass
        };
        db.CatalogItems.Add(item);
        db.SaveChanges();
        return item;
    }
}
