using Microsoft.EntityFrameworkCore;
using TestProject1.Fixtures;
using PsP.Models;

namespace PsP.Tests;

[Collection("db")]
public class CreateOrderFlowTests
{
    private readonly PsP.Data.AppDbContext _db;
    public CreateOrderFlowTests(DatabaseFixture fx) => _db = fx.Db;

    [Fact]
    public async Task CreateOrder_WithTwoLines_Saves_AndLoadsWithLines()
    {
        // Seed a business, employee, and a catalog item
        var biz = new Business
        {
            Name = "Test Biz",
            Address = "Any Street 1",       // add this
            Phone = "+3700000000",          // if required in your model
            Email = "biz@test.local",
            CountryCode = "LT",
            PriceIncludesTax = false        // if present/required
        };        var emp = new Employee { Name = "Alice", Role = "Staff", Status = "Active", Business = biz,Email = "a@b.c",PasswordHash = "whatever" };
        var item = new CatalogItem
        {
            Business = biz,
            Name = "Latte",
            Code = "LATTE",
            Type = "Product",
            Status = "Active",
            TaxClass = "Food",
            BasePrice = 3.20m
        };

        _db.AddRange(biz, emp, item);
        await _db.SaveChangesAsync();

        // Create order
        var order = new Order
        {
            BusinessId = biz.BusinessId,
            EmployeeId = emp.EmployeeId,
            Status = "Open",
            CreatedAt = DateTime.UtcNow,
            TipAmount = 0m,
            TableOrArea = "T1"
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Add two lines (snapshots resolved by server in real app)
        var line1 = new OrderLine
        {
            OrderId = order.OrderId,
            BusinessId = biz.BusinessId,
            CatalogItemId = item.CatalogItemId,
            Qty = 1,
            ItemNameSnapshot = "Latte",
            UnitPriceSnapshot = 3.20m,
            TaxClassSnapshot = "Food",
            TaxRateSnapshotPct = 21.0m,
            PerformedAt = DateTime.UtcNow,
            PerformedByEmployeeId = emp.EmployeeId
        };
        var line2 = new OrderLine
        {
            OrderId = order.OrderId,
            BusinessId = biz.BusinessId,
            CatalogItemId = item.CatalogItemId,
            Qty = 2,
            ItemNameSnapshot = "Latte",
            UnitPriceSnapshot = 3.20m,
            TaxClassSnapshot = "Food",
            TaxRateSnapshotPct = 21.0m,
            PerformedAt = DateTime.UtcNow,
            PerformedByEmployeeId = emp.EmployeeId
        };
        _db.AddRange(line1, line2);
        await _db.SaveChangesAsync();

        // Load back with lines and assert
        var loaded = await _db.Orders
            .Include(o => o.Lines)
            .SingleAsync(o => o.OrderId == order.OrderId);

        Assert.Equal("Open", loaded.Status);
        Assert.Equal(2, loaded.Lines.Count);
        Assert.All(loaded.Lines, l => Assert.Equal("Latte", l.ItemNameSnapshot));
    }
}
