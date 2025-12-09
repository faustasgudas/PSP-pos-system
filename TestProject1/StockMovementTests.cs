using Microsoft.EntityFrameworkCore;

using PsP.Contracts.Orders;
using PsP.Contracts.StockMovements;
using PsP.Data;
using PsP.Models;
using PsP.Services.Implementations;

namespace TestProject1;

public class StockMovementTests
{
    // --------------------------------------------------------
    // Helper: boot an in-memory db + services + seeded product
    // --------------------------------------------------------
    private static (AppDbContext db, StockMovementService sms, OrdersService orders, 
                    Business biz, Employee emp, CatalogItem item, StockItem stock)
        Boot(decimal initialQty = 100m)
    {
        var db = TestDb.NewInMemory();

        // Business
        var biz = new Business
        {
            Name = "Biz",
            Address = "A",
            Phone = "1",
            Email = "a@a.com",
            CountryCode = "LT",
            PriceIncludesTax = true
        };
        db.Businesses.Add(biz);
        db.SaveChanges();

        // Employee
        var emp = new Employee
        {
            BusinessId = biz.BusinessId,
            Name = "Emp",
            Email = "emp@x.com",
            PasswordHash = "x",
            Role = "Staff",
            Status = "Active"
        };
        db.Employees.Add(emp);
        db.SaveChanges();

        // CatalogItem (PRODUCT)
        var item = new CatalogItem
        {
            BusinessId = biz.BusinessId,
            Name = "Coffee",
            Code = "COF",
            Type = "Product",
            BasePrice = 10m,
            Status = "Active",
            TaxClass = "Food"
        };
        db.CatalogItems.Add(item);
        db.SaveChanges();

        // StockItem
        var stock = new StockItem
        {
            CatalogItemId = item.CatalogItemId,
            Unit = "pcs",
            QtyOnHand = initialQty,
            AverageUnitCost = 1m
        };
        db.StockItems.Add(stock);
        db.SaveChanges();

        // Services
        var discounts = new DiscountsService(db);
        var sms = new StockMovementService(db);
        var orders = new OrdersService(db, discounts, sms);

        return (db, sms, orders, biz, emp, item, stock);
    }

    // --------------------------------------------------------
    // 1. SALE reduces stock
    // --------------------------------------------------------
    [Fact]
    public async Task SaleMovement_Decreases_Stock()
    {
        var (db, sms, _, biz, emp, item, stock) = Boot(initialQty: 10);

        await sms.CreateAsync(
            biz.BusinessId,
            stock.StockItemId,
            emp.EmployeeId,
            new CreateStockMovementRequest
            {
                Type = "Sale",
                Delta = -1m
            }
        );

        var updated = await db.StockItems.FindAsync(stock.StockItemId);
        Assert.Equal(9m, updated!.QtyOnHand);
    }

    // --------------------------------------------------------
    // 2. Receive increases stock
    // --------------------------------------------------------
    [Fact]
    public async Task Receive_Increases_Stock()
    {
        var (db, sms, _, biz, emp, _, stock) = Boot(20);

        await sms.CreateAsync(
            biz.BusinessId,
            stock.StockItemId,
            emp.EmployeeId,
            new CreateStockMovementRequest
            {
                Type = "Receive",
                Delta = 30m,
                UnitCostSnapshot = 2.0m
            }
        );

        var updated = await db.StockItems.FindAsync(stock.StockItemId);

        Assert.Equal(50m, updated!.QtyOnHand);
        Assert.Equal(1.6m, updated.AverageUnitCost); // simplified avg cost rule
    }

    // --------------------------------------------------------
    // 3. RefundReturn restores stock
    // --------------------------------------------------------
    [Fact]
    public async Task RefundReturn_Restores_Stock()
    {
        var (db, sms, _, biz, emp, _, stock) = Boot(10);

        await sms.CreateAsync(
            biz.BusinessId,
            stock.StockItemId,
            emp.EmployeeId,
            new CreateStockMovementRequest
            {
                Type = "RefundReturn",
                Delta = +3m
            }
        );

        var updated = await db.StockItems.FindAsync(stock.StockItemId);
        Assert.Equal(13m, updated!.QtyOnHand);
    }

    // --------------------------------------------------------
    // 4. AddLine performs SALE movement
    // --------------------------------------------------------
    [Fact]
    public async Task AddLine_Creates_Sale_Movement()
    {
        var (db, sms, orders, biz, emp, item, stock) = Boot(100);

        var order = await orders.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        var line = await orders.AddLineAsync(
            biz.BusinessId,
            order.OrderId,
            emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 5m }
        );

        var updated = await db.StockItems.FindAsync(stock.StockItemId);
        Assert.Equal(95m, updated!.QtyOnHand);

        var mv = await db.StockMovements.FirstAsync();
        Assert.Equal(-5m, mv.Delta);
        Assert.Equal(line.OrderLineId, mv.OrderLineId);
    }

    // --------------------------------------------------------
    // 5. RemoveLine restores stock (reverse sale)
    // --------------------------------------------------------
    [Fact]
    public async Task RemoveLine_Restores_Stock()
    {
        var (db, _, orders, biz, emp, item, stock) = Boot(50);

        var order = await orders.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        var line = await orders.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 5m });

        await orders.RemoveLineAsync(biz.BusinessId, order.OrderId, line.OrderLineId, emp.EmployeeId);

        var updated = await db.StockItems.FindAsync(stock.StockItemId);
        Assert.Equal(50m, updated!.QtyOnHand); // restored
    }

    // --------------------------------------------------------
    // 6. CancelOrder restores all stock
    // --------------------------------------------------------
    [Fact]
    public async Task CancelOrder_Restores_All_Stock()
    {
        var (db, _, orders, biz, emp, item, stock) = Boot(100);

        var order = await orders.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        await orders.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 7m });

        var updated1 = await db.StockItems.FindAsync(stock.StockItemId);
        Assert.Equal(93m, updated1!.QtyOnHand);

        await orders.CancelOrderAsync(biz.BusinessId, order.OrderId, emp.EmployeeId, new CancelOrderRequest());

        var updated2 = await db.StockItems.FindAsync(stock.StockItemId);
        Assert.Equal(100m, updated2!.QtyOnHand);
    }

    // --------------------------------------------------------
    // 7. Fails if stock item not found
    // --------------------------------------------------------
    [Fact]
    public async Task CreateMovement_Throws_When_StockItemMissing()
    {
        var (db, sms, _, biz, emp, _, _) = Boot();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sms.CreateAsync(
                biz.BusinessId,
                stockItemId: 999,
                callerEmployeeId: emp.EmployeeId,
                new CreateStockMovementRequest { Type = "Sale", Delta = -1 }
            ));
    }

    // --------------------------------------------------------
    // 8. Fails if not enough stock for sale
    // --------------------------------------------------------
    [Fact]
    public async Task AddLine_Throws_When_NotEnoughStock()
    {
        var (db, _, orders, biz, emp, item, stock) = Boot(initialQty: 2);

        var order = await orders.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orders.AddLineAsync(
                biz.BusinessId,
                order.OrderId,
                emp.EmployeeId,
                new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 10m }
            ));
    }
}
