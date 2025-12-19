using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Orders;
using PsP.Data;
using PsP.Models;
using PsP.Services.Implementations;

namespace TestProject1;

public class MoveOrderLinesTests
{
 
    private static async Task<(AppDbContext db, DbConnection conn)> NewSqliteAsync()
    {
        
        return await TestHelpers.TestDbSqlite.NewAsync();
    }

    private static OrdersService NewOrdersService(AppDbContext db)
    {
       
        var discounts = new DiscountsService(db);
        var stock = new StockMovementService(db);
        return new OrdersService(db, discounts, stock);
    }

  
    private static Business SeedBusiness(AppDbContext db)
    {
        var biz = new Business
        {
            Name = "Biz " + Guid.NewGuid().ToString("N")[..6],
            Address = "Addr",
            Phone = "1",
            Email = "biz@test.local",
            CountryCode = "LT",
            PriceIncludesTax = false
        };
        db.Businesses.Add(biz);
        db.SaveChanges();
        return biz;
    }

    private static Employee SeedEmployee(AppDbContext db, int businessId, string role, string name = "Emp")
    {
        var emp = new Employee
        {
            BusinessId = businessId,
            Name = name,
            Role = role,
            Status = "Active",
            Email = Guid.NewGuid().ToString("N")[..6] + "@t.local",
            PasswordHash = "x"
        };
        db.Employees.Add(emp);
        db.SaveChanges();
        return emp;
    }

    private static CatalogItem SeedCatalogItem(AppDbContext db, int businessId, string name = "Coffee")
    {
        var item = new CatalogItem
        {
            BusinessId = businessId,
            Name = name,
            Code = "SKU-" + Guid.NewGuid().ToString("N")[..6],
            Type = "Product",
            BasePrice = 10m,
            Status = "Active",
            DefaultDurationMin = 0,
            TaxClass = "Food"
        };
        db.CatalogItems.Add(item);
        db.SaveChanges();
        return item;
    }

    private static Order SeedOpenOrder(AppDbContext db, int businessId, int employeeId, string? table = "T1")
    {
        var order = new Order
        {
            BusinessId = businessId,
            EmployeeId = employeeId,
            Status = "Open",
            CreatedAt = DateTime.UtcNow,
            TableOrArea = table
        };
        db.Orders.Add(order);
        db.SaveChanges();
        return order;
    }

    private static Order SeedClosedOrder(AppDbContext db, int businessId, int employeeId)
    {
        var order = new Order
        {
            BusinessId = businessId,
            EmployeeId = employeeId,
            Status = "Closed",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ClosedAt = DateTime.UtcNow
        };
        db.Orders.Add(order);
        db.SaveChanges();
        return order;
    }

    private static OrderLine SeedLine(AppDbContext db, int businessId, int orderId, CatalogItem item, int performedByEmpId, decimal qty)
    {
        var line = new OrderLine
        {
            BusinessId = businessId,
            OrderId = orderId,
            CatalogItemId = item.CatalogItemId,
            Qty = qty,

            
            ItemNameSnapshot = item.Name,
            CatalogTypeSnapshot = item.Type,
            UnitPriceSnapshot = item.BasePrice,
            UnitDiscountSnapshot = null,
            TaxClassSnapshot = item.TaxClass,
            TaxRateSnapshotPct = 21m,
            PerformedAt = DateTime.UtcNow,
            PerformedByEmployeeId = performedByEmpId
        };
        db.OrderLines.Add(line);
        db.SaveChanges();
        return line;
    }

    

    [Fact]
    public async Task Move_FullLine_Reassigns_OrderId_NoClone()
    {
        var (db, conn) = await NewSqliteAsync();
        await using var _ = conn;
        await using var __ = db;

        var biz = SeedBusiness(db);
        var staff = SeedEmployee(db, biz.BusinessId, "Staff", "Alice");
        var item = SeedCatalogItem(db, biz.BusinessId);

        var from = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId, "T1");
        var to   = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId, "T1");

        var line = SeedLine(db, biz.BusinessId, from.OrderId, item, staff.EmployeeId, qty: 3m);

        var svc = NewOrdersService(db);

        await svc.MoveLinesAsync(
            businessId: biz.BusinessId,
            fromOrderId: from.OrderId,
            callerEmployeeId: staff.EmployeeId,
            request: new MoveOrderLinesRequest
            {
                TargetOrderId = to.OrderId,
                Lines = { new MoveOrderLineRequest { OrderLineId = line.OrderLineId, Qty = 3m } }
            });

        var lines = await db.OrderLines.AsNoTracking().Where(l => l.BusinessId == biz.BusinessId).ToListAsync();
        Assert.Single(lines);
        Assert.Equal(to.OrderId, lines[0].OrderId);
        Assert.Equal(3m, lines[0].Qty);
    }

    [Fact]
    public async Task Move_PartialQty_ClonesLine_AndDecrementsSource()
    {
        var (db, conn) = await NewSqliteAsync();
        await using var _ = conn;
        await using var __ = db;

        var biz = SeedBusiness(db);
        var staff = SeedEmployee(db, biz.BusinessId, "Staff", "Alice");
        var item = SeedCatalogItem(db, biz.BusinessId);

        var from = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId, "T1");
        var to   = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId, "T1");

        var line = SeedLine(db, biz.BusinessId, from.OrderId, item, staff.EmployeeId, qty: 5m);

        var svc = NewOrdersService(db);

        await svc.MoveLinesAsync(
            biz.BusinessId,
            from.OrderId,
            staff.EmployeeId,
            new MoveOrderLinesRequest
            {
                TargetOrderId = to.OrderId,
                Lines = { new MoveOrderLineRequest { OrderLineId = line.OrderLineId, Qty = 2m } }
            });

        var source = await db.OrderLines.AsNoTracking().SingleAsync(l => l.OrderLineId == line.OrderLineId);
        Assert.Equal(from.OrderId, source.OrderId);
        Assert.Equal(3m, source.Qty);

        var cloned = await db.OrderLines.AsNoTracking()
            .Where(l => l.OrderId == to.OrderId)
            .ToListAsync();

        Assert.Single(cloned);
        Assert.Equal(2m, cloned[0].Qty);

      
        Assert.Equal(source.ItemNameSnapshot, cloned[0].ItemNameSnapshot);
        Assert.Equal(source.UnitPriceSnapshot, cloned[0].UnitPriceSnapshot);
        Assert.Equal(source.TaxClassSnapshot, cloned[0].TaxClassSnapshot);
        Assert.Equal(source.TaxRateSnapshotPct, cloned[0].TaxRateSnapshotPct);
    }

  
    [Fact]
    public async Task Move_Rejects_DuplicateOrderLineId_InRequest()
    {
        var (db, conn) = await NewSqliteAsync();
        await using var _ = conn;
        await using var __ = db;

        var biz = SeedBusiness(db);
        var staff = SeedEmployee(db, biz.BusinessId, "Staff");
        var item = SeedCatalogItem(db, biz.BusinessId);

        var from = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);
        var to   = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);
        var line = SeedLine(db, biz.BusinessId, from.OrderId, item, staff.EmployeeId, 5m);

        var svc = NewOrdersService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.MoveLinesAsync(
                biz.BusinessId, from.OrderId, staff.EmployeeId,
                new MoveOrderLinesRequest
                {
                    TargetOrderId = to.OrderId,
                    Lines =
                    {
                        new MoveOrderLineRequest { OrderLineId = line.OrderLineId, Qty = 1m },
                        new MoveOrderLineRequest { OrderLineId = line.OrderLineId, Qty = 1m }
                    }
                }));

        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Move_Rejects_QtyExceedsAvailable()
    {
        var (db, conn) = await NewSqliteAsync();
        await using var _ = conn;
        await using var __ = db;

        var biz = SeedBusiness(db);
        var staff = SeedEmployee(db, biz.BusinessId, "Staff");
        var item = SeedCatalogItem(db, biz.BusinessId);

        var from = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);
        var to   = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);
        var line = SeedLine(db, biz.BusinessId, from.OrderId, item, staff.EmployeeId, 2m);

        var svc = NewOrdersService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.MoveLinesAsync(
                biz.BusinessId, from.OrderId, staff.EmployeeId,
                new MoveOrderLinesRequest
                {
                    TargetOrderId = to.OrderId,
                    Lines = { new MoveOrderLineRequest { OrderLineId = line.OrderLineId, Qty = 3m } }
                }));

        Assert.Contains("exceeds", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Move_Rejects_LineNotInSourceOrder()
    {
        var (db, conn) = await NewSqliteAsync();
        await using var _ = conn;
        await using var __ = db;

        var biz = SeedBusiness(db);
        var staff = SeedEmployee(db, biz.BusinessId, "Staff");
        var item = SeedCatalogItem(db, biz.BusinessId);

        var from = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);
        var to   = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);

        
        var foreignLine = SeedLine(db, biz.BusinessId, to.OrderId, item, staff.EmployeeId, 1m);

        var svc = NewOrdersService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.MoveLinesAsync(
                biz.BusinessId, from.OrderId, staff.EmployeeId,
                new MoveOrderLinesRequest
                {
                    TargetOrderId = to.OrderId,
                    Lines = { new MoveOrderLineRequest { OrderLineId = foreignLine.OrderLineId, Qty = 1m } }
                }));

        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Move_Rejects_TargetSameAsSource()
    {
        var (db, conn) = await NewSqliteAsync();
        await using var _ = conn;
        await using var __ = db;

        var biz = SeedBusiness(db);
        var staff = SeedEmployee(db, biz.BusinessId, "Staff");
        var item = SeedCatalogItem(db, biz.BusinessId);

        var from = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);
        var line = SeedLine(db, biz.BusinessId, from.OrderId, item, staff.EmployeeId, 1m);

        var svc = NewOrdersService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.MoveLinesAsync(
                biz.BusinessId, from.OrderId, staff.EmployeeId,
                new MoveOrderLinesRequest
                {
                    TargetOrderId = from.OrderId,
                    Lines = { new MoveOrderLineRequest { OrderLineId = line.OrderLineId, Qty = 1m } }
                }));

        Assert.Contains("same", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Move_Rejects_WhenSourceOrderClosed()
    {
        var (db, conn) = await NewSqliteAsync();
        await using var _ = conn;
        await using var __ = db;

        var biz = SeedBusiness(db);
        var staff = SeedEmployee(db, biz.BusinessId, "Staff");
        var item = SeedCatalogItem(db, biz.BusinessId);

        var from = SeedClosedOrder(db, biz.BusinessId, staff.EmployeeId);
        var to   = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);
        var line = SeedLine(db, biz.BusinessId, from.OrderId, item, staff.EmployeeId, 1m);

        var svc = NewOrdersService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.MoveLinesAsync(
                biz.BusinessId, from.OrderId, staff.EmployeeId,
                new MoveOrderLinesRequest
                {
                    TargetOrderId = to.OrderId,
                    Lines = { new MoveOrderLineRequest { OrderLineId = line.OrderLineId, Qty = 1m } }
                }));

        Assert.Contains("OPEN", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Move_Rejects_WhenTargetOrderClosed()
    {
        var (db, conn) = await NewSqliteAsync();
        await using var _ = conn;
        await using var __ = db;

        var biz = SeedBusiness(db);
        var staff = SeedEmployee(db, biz.BusinessId, "Staff");
        var item = SeedCatalogItem(db, biz.BusinessId);

        var from = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);
        var to   = SeedClosedOrder(db, biz.BusinessId, staff.EmployeeId);
        var line = SeedLine(db, biz.BusinessId, from.OrderId, item, staff.EmployeeId, 1m);

        var svc = NewOrdersService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.MoveLinesAsync(
                biz.BusinessId, from.OrderId, staff.EmployeeId,
                new MoveOrderLinesRequest
                {
                    TargetOrderId = to.OrderId,
                    Lines = { new MoveOrderLineRequest { OrderLineId = line.OrderLineId, Qty = 1m } }
                }));

        Assert.Contains("OPEN", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Staff_Cannot_Move_Between_OtherPeoples_Orders_But_Manager_Can()
    {
        var (db, conn) = await NewSqliteAsync();
        await using var _ = conn;
        await using var __ = db;

        var biz = SeedBusiness(db);
        var alice = SeedEmployee(db, biz.BusinessId, "Staff", "Alice");
        var bob   = SeedEmployee(db, biz.BusinessId, "Staff", "Bob");
        var mgr   = SeedEmployee(db, biz.BusinessId, "Manager", "Boss");
        var item = SeedCatalogItem(db, biz.BusinessId);

        var from = SeedOpenOrder(db, biz.BusinessId, bob.EmployeeId);
        var to   = SeedOpenOrder(db, biz.BusinessId, bob.EmployeeId);
        var line = SeedLine(db, biz.BusinessId, from.OrderId, item, bob.EmployeeId, 1m);

        var svc = NewOrdersService(db);

       
        var ex1 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.MoveLinesAsync(
                biz.BusinessId, from.OrderId, alice.EmployeeId,
                new MoveOrderLinesRequest
                {
                    TargetOrderId = to.OrderId,
                    Lines = { new MoveOrderLineRequest { OrderLineId = line.OrderLineId, Qty = 1m } }
                }));
        Assert.Contains("Forbidden", ex1.Message, StringComparison.OrdinalIgnoreCase);

        
        await svc.MoveLinesAsync(
            biz.BusinessId, from.OrderId, mgr.EmployeeId,
            new MoveOrderLinesRequest
            {
                TargetOrderId = to.OrderId,
                Lines = { new MoveOrderLineRequest { OrderLineId = line.OrderLineId, Qty = 1m } }
            });

        var updated = await db.OrderLines.AsNoTracking().SingleAsync(l => l.OrderLineId == line.OrderLineId);
        Assert.Equal(to.OrderId, updated.OrderId);
    }
    
    [Fact]
public async Task Move_Preserves_LineDiscountSnapshot_And_DiscountId_On_FullMove_And_Split()
{
    var (db, conn) = await NewSqliteAsync();
    await using var _ = conn;
    await using var __ = db;

    var biz = SeedBusiness(db);
    var staff = SeedEmployee(db, biz.BusinessId, "Staff");
    var item = SeedCatalogItem(db, biz.BusinessId);

    var from = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);
    var to   = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);

    
    var line = SeedLine(db, biz.BusinessId, from.OrderId, item, staff.EmployeeId, qty: 5m);
    line.DiscountId = 777;
    line.UnitDiscountSnapshot = "{\"v\":1,\"scope\":\"Line\",\"type\":\"Percent\",\"value\":10}";
    db.Update(line);
    await db.SaveChangesAsync();

    var svc = NewOrdersService(db);

    
    await svc.MoveLinesAsync(
        biz.BusinessId, from.OrderId, staff.EmployeeId,
        new MoveOrderLinesRequest
        {
            TargetOrderId = to.OrderId,
            Lines = { new MoveOrderLineRequest { OrderLineId = line.OrderLineId, Qty = 2m } }
        });

    var source = await db.OrderLines.AsNoTracking().SingleAsync(l => l.OrderLineId == line.OrderLineId);
    var moved  = await db.OrderLines.AsNoTracking().SingleAsync(l => l.OrderId == to.OrderId);

    Assert.Equal(3m, source.Qty);
    Assert.Equal(2m, moved.Qty);

 
    Assert.Equal(777, source.DiscountId);
    Assert.Equal(777, moved.DiscountId);
    Assert.Equal(source.UnitDiscountSnapshot, moved.UnitDiscountSnapshot);
}

[Fact]
public async Task Move_DoesNot_Modify_OrderLevelDiscountSnapshot_On_Source_Or_Target()
{
    var (db, conn) = await NewSqliteAsync();
    await using var _ = conn;
    await using var __ = db;

    var biz = SeedBusiness(db);
    var staff = SeedEmployee(db, biz.BusinessId, "Staff");
    var item = SeedCatalogItem(db, biz.BusinessId);

    var from = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);
    var to   = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);

    from.OrderDiscountSnapshot = "{\"v\":1,\"scope\":\"Order\",\"type\":\"Percent\",\"value\":5}";
    to.OrderDiscountSnapshot   = "{\"v\":1,\"scope\":\"Order\",\"type\":\"Amount\",\"value\":2}";
    db.Update(from);
    db.Update(to);
    await db.SaveChangesAsync();

    var line = SeedLine(db, biz.BusinessId, from.OrderId, item, staff.EmployeeId, qty: 1m);

    var svc = NewOrdersService(db);

    await svc.MoveLinesAsync(
        biz.BusinessId, from.OrderId, staff.EmployeeId,
        new MoveOrderLinesRequest
        {
            TargetOrderId = to.OrderId,
            Lines = { new MoveOrderLineRequest { OrderLineId = line.OrderLineId, Qty = 1m } }
        });

    var from2 = await db.Orders.AsNoTracking().SingleAsync(o => o.OrderId == from.OrderId);
    var to2   = await db.Orders.AsNoTracking().SingleAsync(o => o.OrderId == to.OrderId);

    Assert.Equal("{\"v\":1,\"scope\":\"Order\",\"type\":\"Percent\",\"value\":5}", from2.OrderDiscountSnapshot);
    Assert.Equal("{\"v\":1,\"scope\":\"Order\",\"type\":\"Amount\",\"value\":2}", to2.OrderDiscountSnapshot);
}

[Fact]
public async Task Move_IsAtomic_When_SecondLineFails_NothingChanges()
{
    var (db, conn) = await NewSqliteAsync();
    await using var _ = conn;
    await using var __ = db;

    var biz = SeedBusiness(db);
    var staff = SeedEmployee(db, biz.BusinessId, "Staff");
    var item = SeedCatalogItem(db, biz.BusinessId);

    var from = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);
    var to   = SeedOpenOrder(db, biz.BusinessId, staff.EmployeeId);

    var l1 = SeedLine(db, biz.BusinessId, from.OrderId, item, staff.EmployeeId, qty: 5m);
    var l2 = SeedLine(db, biz.BusinessId, from.OrderId, item, staff.EmployeeId, qty: 1m);

    var svc = NewOrdersService(db);

    
    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        svc.MoveLinesAsync(
            biz.BusinessId, from.OrderId, staff.EmployeeId,
            new MoveOrderLinesRequest
            {
                TargetOrderId = to.OrderId,
                Lines =
                {
                    new MoveOrderLineRequest { OrderLineId = l1.OrderLineId, Qty = 2m },
                    new MoveOrderLineRequest { OrderLineId = l2.OrderLineId, Qty = 999m }
                }
            }));

   
    var lines = await db.OrderLines.AsNoTracking().Where(x => x.BusinessId == biz.BusinessId).ToListAsync();
    Assert.Equal(2, lines.Count);
    Assert.All(lines, x => Assert.Equal(from.OrderId, x.OrderId));
    Assert.Equal(5m, lines.Single(x => x.OrderLineId == l1.OrderLineId).Qty);
    Assert.Equal(1m, lines.Single(x => x.OrderLineId == l2.OrderLineId).Qty);
}
    
    
    
}