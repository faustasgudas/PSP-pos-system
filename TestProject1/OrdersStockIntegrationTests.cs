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
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) 
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Full_Order_Stock_Discount_Flow_Works()
    {
        using var db = CreateDbContext();

        var ct = CancellationToken.None;

        
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

            
            Email        = "owner@test.local",
            PasswordHash = "dummy-hash",   
           
        };
        db.Employees.Add(owner);
        await db.SaveChangesAsync(ct);

       

        var catalogItem = new CatalogItem
        {
            BusinessId = business.BusinessId,
            Name       = "Beer 0.5L",
            Code       = "BEER-05",
            Type       = "Product",      
            Status     = "Active",
            TaxClass   = "Food",         
            BasePrice  = 5.0m,
            DefaultDurationMin = 0      
        };
        db.CatalogItems.Add(catalogItem);
        await db.SaveChangesAsync(ct);

        

        var stockItem = new StockItem
        {
            CatalogItemId    = catalogItem.CatalogItemId,
            Unit             = "pcs",
            QtyOnHand        = 10m,
            AverageUnitCost  = 2.0m
        };
        db.StockItems.Add(stockItem);
        await db.SaveChangesAsync(ct);

        

        var discounts      = new DiscountsService(db);
        var stockMovements = new StockMovementService(db);
        var orders         = new OrdersService(db, discounts, stockMovements);

        

        var createOrderReq = new CreateOrderRequest
        {
            EmployeeId  = owner.EmployeeId,
            TableOrArea = "T1"
           
        };

        var orderDetail = await orders.CreateOrderAsync(
            business.BusinessId,
            callerEmployeeId: owner.EmployeeId,
            createOrderReq,
            ct);

        var orderId = orderDetail.OrderId;

        

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

        Assert.Equal(8m, stockAfterFirstSale.QtyOnHand); 

        var movementsAfterFirstSale = await db.StockMovements
            .AsNoTracking()
            .Where(m => m.StockItemId == stockItem.StockItemId)
            .OrderBy(m => m.StockMovementId)
            .ToListAsync(ct);

        Assert.Single(movementsAfterFirstSale);
        Assert.Equal("Sale", movementsAfterFirstSale[0].Type);
        Assert.Equal(-2m,   movementsAfterFirstSale[0].Delta);
        Assert.Equal(orderLineId, movementsAfterFirstSale[0].OrderLineId);

        

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

       

        var cancelReq = new CancelOrderRequest
        {
            EmployeeId = owner.EmployeeId,            
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

       
        Assert.Equal(10m, stockAfterCancel.QtyOnHand);

        var movementsAfterCancel = await db.StockMovements
            .AsNoTracking()
            .Where(m => m.StockItemId == stockItem.StockItemId)
            .OrderBy(m => m.StockMovementId)
            .ToListAsync(ct);

       
        Assert.Equal(3, movementsAfterCancel.Count);

        var refundMove = movementsAfterCancel.Last();
        Assert.Equal("Adjust", refundMove.Type);
        Assert.Equal(3m,             refundMove.Delta);
    }
}
