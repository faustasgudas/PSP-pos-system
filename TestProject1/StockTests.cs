using Microsoft.EntityFrameworkCore;
using PsP.Models;
using TestProject1;
using Npgsql;
namespace PsP.Tests;

public class StockTests
{
    [Fact]
    public async Task StockItem_IsUniquePerCatalogItem()
    {
        await using var db1 = TestHelpers.NewContext();
        var (biz, _) = TestHelpers.SeedBusinessAndEmployee(db1);
        var item = TestHelpers.SeedCatalogItem(db1, biz.BusinessId);

      
        db1.StockItems.Add(new StockItem
        {
            CatalogItemId = item.CatalogItemId,
            Unit = "pcs",
            QtyOnHand = 0,
            AverageUnitCost = 0
        });
        await db1.SaveChangesAsync();

        
        await using var db2 = TestHelpers.NewContext();

        db2.StockItems.Add(new StockItem
        {
            CatalogItemId = item.CatalogItemId, 
            Unit = "pcs",
            QtyOnHand = 0,
            AverageUnitCost = 0
        });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());
        var pg = ex.InnerException as PostgresException;
        Assert.NotNull(pg);               
        Assert.Equal("23505", pg!.SqlState); 
    }

    [Fact]
    public async Task StockMovement_LinksToOrderLine_WhenSale()
    {
        await using var db = TestHelpers.NewContext();
        var (biz, emp) = TestHelpers.SeedBusinessAndEmployee(db);
        var item = TestHelpers.SeedCatalogItem(db, biz.BusinessId, "Tea", "Food");

        var stock = new StockItem
        {
            CatalogItemId = item.CatalogItemId,
            Unit = "pcs",
            QtyOnHand = 10m,
            AverageUnitCost = 0.50m
        };
        db.StockItems.Add(stock);
        await db.SaveChangesAsync();

        var order = new Order
        {
            BusinessId = biz.BusinessId,
            EmployeeId = emp.EmployeeId,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var line = new OrderLine
        {
            BusinessId = biz.BusinessId,
            OrderId = order.OrderId,
            CatalogItemId = item.CatalogItemId,
            Qty = 2m,
            ItemNameSnapshot = item.Name,
            UnitPriceSnapshot = item.BasePrice,
            TaxClassSnapshot = item.TaxClass,
            TaxRateSnapshotPct = 21m,
            PerformedAt = DateTime.UtcNow,
            PerformedByEmployeeId = emp.EmployeeId
        };
        db.OrderLines.Add(line);
        await db.SaveChangesAsync();

        var move = new StockMovement
        {
            StockItemId = stock.StockItemId,
            OrderLineId = line.OrderLineId,
            Type = "Sale",
            Delta = -2m,
            UnitCostSnapshot = stock.AverageUnitCost,
            At = DateTime.UtcNow
        };
        db.StockMovements.Add(move);
        await db.SaveChangesAsync();

        var loaded = await db.StockMovements
            .Include(m => m.StockItem)
            .FirstAsync(m => m.StockMovementId == move.StockMovementId);

        Assert.Equal("Sale", loaded.Type);
        Assert.Equal(line.OrderLineId, loaded.OrderLineId);
        Assert.Equal(stock.StockItemId, loaded.StockItemId);
    }
    
    [Fact]
    public async Task StockItem_AllowsDifferentCatalogItems()
    {
        await using var db = TestHelpers.NewContext();
        var (biz, _) = TestHelpers.SeedBusinessAndEmployee(db);
        var itemA = TestHelpers.SeedCatalogItem(db, biz.BusinessId);
        var itemB = TestHelpers.SeedCatalogItem(db, biz.BusinessId);

        db.StockItems.Add(new StockItem
        {
            CatalogItemId = itemA.CatalogItemId,
            Unit = "pcs",
            QtyOnHand = 0,
            AverageUnitCost = 0
        });
        db.StockItems.Add(new StockItem
        {
            CatalogItemId = itemB.CatalogItemId, 
            Unit = "pcs",
            QtyOnHand = 0,
            AverageUnitCost = 0
        });

        await db.SaveChangesAsync(); 
    }
    
    [Fact]
    public void StockItem_HasUniqueIndex_OnModel()
    {
        using var db = TestHelpers.NewContext();
        var entity = db.Model.FindEntityType(typeof(StockItem))!;
        var indexes = entity.GetIndexes();

     
        var idx = indexes.Single(i => i.Properties.Any(p => p.Name == nameof(StockItem.CatalogItemId)));
        Assert.True(idx.IsUnique);
    }
    
    
    
    
}
