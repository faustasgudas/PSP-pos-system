using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Orders;
using PsP.Contracts.StockItems;
using PsP.Contracts.StockMovements;
using PsP.Data;
using PsP.Models;
using PsP.Services.Implementations;
using Xunit;

namespace PsP.Tests.Integration;

public class OrdersStockIntegrationTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // isolated DB per test
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Full_Order_Stock_Discount_Flow_Works()
    {
        using var db = CreateDbContext();

        var ct = CancellationToken.None;

        // === Arrange base data: business + owner employee =====================

        var business = new Business
        {
            Name = "Test Biz",
            Address = "Somewhere",
            Phone = "123",
            Email = "test@biz.local",
            CountryCode = "LT",
            PriceIncludesTax = true,
            BusinessType = "Catering",
            BusinessStatus = "Active"
        };
        db.Businesses.Add(business);
        await db.SaveChangesAsync(ct);

        var owner = new Employee
        {
            BusinessId = business.BusinessId,
            Name       = "Owner",
            Role       = "Owner",
            Status     = "Active",

            // ✅ required properties on Employee entity:
            Email        = "owner@test.local",
            PasswordHash = "dummy-hash",   // any non-empty string is enough for tests
            // add any other non-nullable props if your Employee model has them
        };
        db.Employees.Add(owner);
        await db.SaveChangesAsync(ct);

        // === Arrange catalog item (product) ===================================

        var catalogItem = new CatalogItem
        {
            BusinessId = business.BusinessId,
            Name       = "Beer 0.5L",
            Code       = "BEER-05",
            Type       = "Product",      // important: product so stock is used
            Status     = "Active",
            TaxClass   = "Food",         // or whatever you use
            BasePrice  = 5.0m,
            DefaultDurationMin = 0       // since it's a product
        };
        db.CatalogItems.Add(catalogItem);
        await db.SaveChangesAsync(ct);

        // === Arrange StockItem with initial qty ===============================

        var stockItem = new StockItem
        {
            CatalogItemId    = catalogItem.CatalogItemId,
            Unit             = "pcs",
            QtyOnHand        = 10m,
            AverageUnitCost  = 2.0m
        };
        db.StockItems.Add(stockItem);
        await db.SaveChangesAsync(ct);

        // === Create services (using real EF) =================================

        var discounts      = new DiscountsService(db);
        var stockMovements = new StockMovementService(db);
        var orders         = new OrdersService(db, discounts, stockMovements);

        // === 1) Create order ==================================================

        var createOrderReq = new CreateOrderRequest
        {
            EmployeeId  = owner.EmployeeId,
            TableOrArea = "T1"
            // ReservationId = null
        };

        var orderDetail = await orders.CreateOrderAsync(
            business.BusinessId,
            callerEmployeeId: owner.EmployeeId,
            createOrderReq,
            ct);

        var orderId = orderDetail.OrderId;

        // === 2) Add line (qty 2) -> stock 10 -> 8, Sale movement =============

        var addLineReq = new AddLineRequest
        {
            CatalogItemId = catalogItem.CatalogItemId,
            Qty           = 2m
        };

        var lineResp = await orders.AddLineAsync(
            business.BusinessId,
            orderId,
            callerEmployeeId: owner.EmployeeId,
            addLineReq,
            ct);

        var orderLineId = lineResp.OrderLineId;

        var stockAfterFirstSale = await db.StockItems
            .AsNoTracking()
            .FirstAsync(s => s.StockItemId == stockItem.StockItemId, ct);

        Assert.Equal(8m, stockAfterFirstSale.QtyOnHand); // 10 - 2

        var movementsAfterFirstSale = await db.StockMovements
            .AsNoTracking()
            .Where(m => m.StockItemId == stockItem.StockItemId)
            .OrderBy(m => m.StockMovementId)
            .ToListAsync(ct);

        Assert.Single(movementsAfterFirstSale);
        Assert.Equal("Sale", movementsAfterFirstSale[0].Type);
        Assert.Equal(-2m,   movementsAfterFirstSale[0].Delta);
        Assert.Equal(orderLineId, movementsAfterFirstSale[0].OrderLineId);

        // === 3) Update line: increase from 2 -> 3 ============================

        var updateReq = new UpdateLineRequest
        {
            Qty = 3m
        };

        var updatedLine = await orders.UpdateLineAsync(
            business.BusinessId,
            orderId,
            orderLineId,
            callerEmployeeId: owner.EmployeeId,
            updateReq,
            ct);

        var stockAfterUpdate = await db.StockItems
            .AsNoTracking()
            .FirstAsync(s => s.StockItemId == stockItem.StockItemId, ct);

        // diff = +1 → 8 - 1 = 7
        Assert.Equal(7m, stockAfterUpdate.QtyOnHand);

        var movementsAfterUpdate = await db.StockMovements
            .AsNoTracking()
            .Where(m => m.StockItemId == stockItem.StockItemId)
            .OrderBy(m => m.StockMovementId)
            .ToListAsync(ct);

        Assert.Equal(2, movementsAfterUpdate.Count);

        var secondMove = movementsAfterUpdate[1];
        Assert.Equal("Sale", secondMove.Type);
        Assert.Equal(-1m,    secondMove.Delta);
        Assert.Equal(orderLineId, secondMove.OrderLineId);

        // === 4) Cancel order -> stock restored with RefundReturn =============

        var cancelReq = new CancelOrderRequest
        {
            EmployeeId = owner.EmployeeId,             // just to be consistent with contract
            Reason     = "Integration test cancel"
        };

        var cancelledOrder = await orders.CancelOrderAsync(
            business.BusinessId,
            orderId,
            callerEmployeeId: owner.EmployeeId,
            cancelReq,
            ct);

        var stockAfterCancel = await db.StockItems
            .AsNoTracking()
            .FirstAsync(s => s.StockItemId == stockItem.StockItemId, ct);

        // 7 on hand, order qty 3 → 7 + 3 = 10
        Assert.Equal(10m, stockAfterCancel.QtyOnHand);

        var movementsAfterCancel = await db.StockMovements
            .AsNoTracking()
            .Where(m => m.StockItemId == stockItem.StockItemId)
            .OrderBy(m => m.StockMovementId)
            .ToListAsync(ct);

        // 1 Sale (-2) + 1 Sale (-1) + 1 RefundReturn (+3) = 3 rows
        Assert.Equal(3, movementsAfterCancel.Count);

        var refundMove = movementsAfterCancel.Last();
        Assert.Equal("RefundReturn", refundMove.Type);
        Assert.Equal(3m,             refundMove.Delta);
    }
}
