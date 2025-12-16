using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Orders;
using PsP.Data;
using PsP.Models;
using PsP.Services.Implementations;
using PsP.Services.Interfaces;

namespace TestProject1;

public class OrderServiceTests2
{
    private static (AppDbContext db, IOrdersService orders, IDiscountsService discounts) Boot()
    {
        var db = TestDb.NewInMemory();
        var discounts = new DiscountsService(db);
        var stocks = new StockMovementService(db);// real discounts service
        var orders = new OrdersService(db, discounts,stocks); // real orders service (matches prod logic)
        return (db, orders,discounts);
    }

    [Fact]
    public async Task ListMine_ReturnsOnlyOpenOwned()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var other = Seed.AddEmployee(db, biz.BusinessId, "Staff", "E2");

        // mine (Open & Closed)
        db.Orders.AddRange(
            new Order { BusinessId = biz.BusinessId, EmployeeId = emp.EmployeeId, Status="Open",    CreatedAt=DateTime.UtcNow.AddMinutes(-1) },
            new Order { BusinessId = biz.BusinessId, EmployeeId = emp.EmployeeId, Status="Closed",  CreatedAt=DateTime.UtcNow.AddMinutes(-2) }
        );
        // not mine
        db.Orders.Add(new Order { BusinessId = biz.BusinessId, EmployeeId = other.EmployeeId, Status="Open", CreatedAt=DateTime.UtcNow.AddMinutes(-3) });
        await db.SaveChangesAsync();

        var list = await svc.ListMineAsync(biz.BusinessId, emp.EmployeeId);
        list.Should().HaveCount(1);
        list.First().Status.Should().Be("Open");
    }

    [Fact]
    public async Task ListAll_Requires_ManagerOrOwner()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db); // Staff

        await FluentActions.Invoking(() => svc.ListAllAsync(biz.BusinessId, emp.EmployeeId, null, null, null))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*only managers/owners*");
    }

    [Fact]
    public async Task ListAll_Manager_Ok_WithFilters()
    {
        var (db, svc,_) = Boot();
        var (biz, _) = Seed.BizAndStaff(db);
        var mgr = Seed.AddEmployee(db, biz.BusinessId, "Manager");

        db.Orders.AddRange(
            new Order { BusinessId = biz.BusinessId, EmployeeId = mgr.EmployeeId, Status="Open",   CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new Order { BusinessId = biz.BusinessId, EmployeeId = mgr.EmployeeId, Status="Closed", CreatedAt = DateTime.UtcNow.AddHours(-1) }
        );
        await db.SaveChangesAsync();

        var openOnly = await svc.ListAllAsync(biz.BusinessId, mgr.EmployeeId, "Open", null, null);
        openOnly.Should().HaveCount(1);
        openOnly.First().Status.Should().Be("Open");
    }

    [Fact]
    public async Task CreateOrder_Staff_ForSelf_Ok_AutoOrderDiscountSnapshot_IfAvailable()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        // add an order-level discount (active newest)
        Seed.OrderDiscount(db, biz.BusinessId, 7.5m);

        var dto = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId, TableOrArea = "T1" });

        dto.EmployeeId.Should().Be(emp.EmployeeId);
        dto.OrderDiscountSnapshot.Should().NotBeNullOrEmpty(); // snapshot present
    }

    [Fact]
    public async Task CreateOrder_Staff_ForOther_Forbidden_Manager_Can()
    {
        var (db, svc,_) = Boot();
        var (biz, staff) = Seed.BizAndStaff(db);
        var other = Seed.AddEmployee(db, biz.BusinessId, "Staff", "E2");
        var mgr = Seed.AddEmployee(db, biz.BusinessId, "Manager", "Boss");

        // staff trying to assign different employee -> forbidden
        await FluentActions.Invoking(() => svc.CreateOrderAsync(biz.BusinessId, staff.EmployeeId,
            new CreateOrderRequest { EmployeeId = other.EmployeeId }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*managers/owners*");

        // manager can
        var ok = await svc.CreateOrderAsync(biz.BusinessId, mgr.EmployeeId,
            new CreateOrderRequest { EmployeeId = other.EmployeeId });
        ok.EmployeeId.Should().Be(other.EmployeeId);
    }

    [Fact]
    public async Task CreateOrder_WithReservation_Verifies_Existence_In_Business()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var item = Seed.Item(db, biz.BusinessId);
        var res = Seed.Reservation(db, biz.BusinessId, emp.EmployeeId, item.CatalogItemId);

        var dto = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId, ReservationId = res.ReservationId });

        dto.ReservationId.Should().Be(res.ReservationId);

        // wrong business / non-existent
        await FluentActions.Invoking(() => svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId, ReservationId = res.ReservationId + 999 }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Reservation not found*");
    }

    [Fact]
    public async Task GetOrder_And_Lines_Enforce_Ownership_For_Staff()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var other = Seed.AddEmployee(db, biz.BusinessId, "Staff", "E2");

        var foreignOrder = new Order { BusinessId = biz.BusinessId, EmployeeId = other.EmployeeId, Status="Open", CreatedAt = DateTime.UtcNow };
        db.Orders.Add(foreignOrder);
        await db.SaveChangesAsync();

        await FluentActions.Invoking(() => svc.GetOrderAsync(biz.BusinessId, foreignOrder.OrderId, emp.EmployeeId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Forbidden*");
    }

    [Fact]
    public async Task AddLine_AutoLineDiscountSnapshot_And_TaxSnapshot()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db, country: "LT");
        var item = Seed.Item(db, biz.BusinessId, taxClass: "Food", price: 20m);
        // tax rule
        Seed.Tax(db, country: biz.CountryCode, taxClass: item.TaxClass, rate: 21m);
        // item-level discount
        Seed.LineDiscountForItem(db, biz.BusinessId, item.CatalogItemId, 10m);

        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        var line = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 2m });

        // verify stored snapshots
        var row = await db.OrderLines.AsNoTracking().FirstAsync(l => l.OrderLineId == line.OrderLineId);
        row.TaxRateSnapshotPct.Should().Be(21m);
        row.UnitDiscountSnapshot.Should().NotBeNull(); // snapshot present because discount exists
        row.DiscountId.Should().NotBeNull();
    }

    [Fact]
    public async Task AddLine_NoTaxRule_Defaults_To_Zero()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db, country: "LT");
        var item = Seed.Item(db, biz.BusinessId, taxClass: "Service", price: 50m);
        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        var line = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m });

        var row = await db.OrderLines.AsNoTracking().FirstAsync(l => l.OrderLineId == line.OrderLineId);
        row.TaxRateSnapshotPct.Should().Be(0m);
    }

    [Fact]
    public async Task UpdateOrder_Staff_Cannot_Change_Employee_To_Other_Manager_Can()
    {
        var (db, svc,_) = Boot();
        var (biz, staff) = Seed.BizAndStaff(db);
        var other = Seed.AddEmployee(db, biz.BusinessId, "Staff", "E2");
        var mgr = Seed.AddEmployee(db, biz.BusinessId, "Manager", "Boss");

        var order = await svc.CreateOrderAsync(biz.BusinessId, staff.EmployeeId,
            new CreateOrderRequest { EmployeeId = staff.EmployeeId });

        // staff attempts to assign to someone else -> forbidden
        await FluentActions.Invoking(() => svc.UpdateOrderAsync(biz.BusinessId, order.OrderId, staff.EmployeeId,
            new UpdateOrderRequest { EmployeeId = other.EmployeeId }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*only managers/owners can update order for others*");

        // manager can reassign
        var ok = await svc.UpdateOrderAsync(biz.BusinessId, order.OrderId, mgr.EmployeeId,
            new UpdateOrderRequest { EmployeeId = other.EmployeeId });
        ok.EmployeeId.Should().Be(other.EmployeeId);
    }

    [Fact]
    public async Task UpdateOrder_SetOrderDiscount_WritesSnapshot()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        var d = Seed.OrderDiscount(db, biz.BusinessId, 12.5m);
        var updated = await svc.UpdateOrderAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new UpdateOrderRequest { EmployeeId = emp.EmployeeId, DiscountId = d.DiscountId });

        updated.OrderDiscountSnapshot.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateLine_SetDiscount_Revalidates_And_CapturesNewSnapshot()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var item = Seed.Item(db, biz.BusinessId);
        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });
        var line = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m });

        var disc = Seed.LineDiscountForItem(db, biz.BusinessId, item.CatalogItemId, 15m);

        var upd = await svc.UpdateLineAsync(biz.BusinessId, order.OrderId, line.OrderLineId, emp.EmployeeId,
            new UpdateLineRequest { Qty = 2m, DiscountId = disc.DiscountId });

        var row = await db.OrderLines.AsNoTracking().FirstAsync(l => l.OrderLineId == upd.OrderLineId);
        row.UnitDiscountSnapshot.Should().NotBeNullOrEmpty();
        row.Qty.Should().Be(2m);
    }

    [Fact]
    public async Task Close_And_Cancel_Work_Only_When_Open()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        var closed = await svc.CloseOrderAsync(biz.BusinessId, order.OrderId, emp.EmployeeId);
        closed.Status.Should().Be("Closed");
        closed.ClosedAt.Should().NotBeNull();

        // cancel now should fail (not open)
        await FluentActions.Invoking(() =>
            svc.CancelOrderAsync(biz.BusinessId, order.OrderId, emp.EmployeeId, new CancelOrderRequest()))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*only for OPEN orders*");
    }

    [Fact]
    public async Task RemoveLine_Deletes()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var item = Seed.Item(db, biz.BusinessId);
        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });
        var line = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m });

        await svc.RemoveLineAsync(biz.BusinessId, order.OrderId, line.OrderLineId, emp.EmployeeId);
        (await db.OrderLines.CountAsync()).Should().Be(0);
    }
    
    
    [Fact]
    public async Task ListLines_Staff_OwnOrder_ReturnsLines()
    {
        var (db, svc, _) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var item = Seed.Item(db, biz.BusinessId);

        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 2m });

        var lines = await svc.ListLinesAsync(biz.BusinessId, order.OrderId, emp.EmployeeId);
        lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListLines_Staff_NotOwner_Forbidden()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var other = Seed.AddEmployee(db, biz.BusinessId, "Staff", "B");
        var item = Seed.Item(db, biz.BusinessId);

        var order = await svc.CreateOrderAsync(biz.BusinessId, other.EmployeeId,
            new CreateOrderRequest { EmployeeId = other.EmployeeId });

        await FluentActions.Invoking(() =>
            svc.ListLinesAsync(biz.BusinessId, order.OrderId, emp.EmployeeId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Forbidden*");
    }

    [Fact]
    public async Task GetLine_Ok_ForOwner()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var item = Seed.Item(db, biz.BusinessId);

        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        var added = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m });

        var line = await svc.GetLineAsync(biz.BusinessId, order.OrderId, added.OrderLineId, emp.EmployeeId);
        line.OrderLineId.Should().Be(added.OrderLineId);
    }

    [Fact]
    public async Task GetLine_NotOwner_Forbidden()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var other = Seed.AddEmployee(db, biz.BusinessId, "Staff", "B");
        var item = Seed.Item(db, biz.BusinessId);

        var order = await svc.CreateOrderAsync(biz.BusinessId, other.EmployeeId,
            new CreateOrderRequest { EmployeeId = other.EmployeeId });

        var added = await svc.AddLineAsync(biz.BusinessId, order.OrderId, other.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m });

        await FluentActions.Invoking(() =>
            svc.GetLineAsync(biz.BusinessId, order.OrderId, added.OrderLineId, emp.EmployeeId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Forbidden*");
    }

    [Fact]
    public async Task GetLine_NotFound_Throws()
    {
        var (db, svc,_) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);

        await FluentActions.Invoking(() =>
            svc.GetLineAsync(biz.BusinessId, orderId: 999, orderLineId: 1, callerEmployeeId: emp.EmployeeId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Order not found*");
    }

    // ---------- AddLine / UpdateLine edge cases (exercise discount math branches) ----------

    [Fact]
    public async Task AddLine_WithPercentDiscount_ComputesSnapshot_AndUsesTaxRule()
    {
        var (db, svc,discounts) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db, country: "LT");

        var item = Seed.Item(db, biz.BusinessId, taxClass: "Food", price: 100m);
        // tax
        Seed.Tax(db, biz.CountryCode, item.TaxClass, rate: 21m);
        // percent line discount
        var d = Seed.LineDiscountForItem(db, biz.BusinessId, item.CatalogItemId, val: 10m);

        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        var line = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m });

        // verify the snapshot JSON carries the discount definition we expect
        var row = await db.OrderLines.AsNoTracking().FirstAsync(x => x.OrderLineId == line.OrderLineId);
        row.UnitPriceSnapshot.Should().Be(100m);
        row.TaxRateSnapshotPct.Should().Be(21m);
        row.DiscountId.Should().Be(d.DiscountId);

        var parsed = discounts.TryParseDiscountSnapshot(row.UnitDiscountSnapshot);
        parsed.Should().NotBeNull();
        parsed!.Type.Should().Be("Percent");
        parsed.Value.Should().Be(10m);
        parsed.CatalogItemId.Should().Be(item.CatalogItemId);
    }

    [Fact]
    public async Task UpdateLine_SetAmountDiscount_ReplacesSnapshot()
    {
        var (db, svc,discounts) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);

        var item = Seed.Item(db, biz.BusinessId, price: 25m);
        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });
        var line = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 2m });

        // create an AMOUNT line discount by setting Type="Amount"
        var disc = new Discount
        {
            BusinessId = biz.BusinessId, Code = "AMT5", Type = "Amount", Scope = "Line", Value = 5m,
            StartsAt = DateTime.UtcNow.AddDays(-1), EndsAt = DateTime.UtcNow.AddDays(10), Status = "Active"
        };
        db.Discounts.Add(disc);
        db.SaveChanges();
        db.DiscountEligibilities.Add(new DiscountEligibility { DiscountId = disc.DiscountId, CatalogItemId = item.CatalogItemId });
        db.SaveChanges();

        var updated = await svc.UpdateLineAsync(biz.BusinessId, order.OrderId, line.OrderLineId, emp.EmployeeId,
            new UpdateLineRequest { DiscountId = disc.DiscountId });

        var row = await db.OrderLines.AsNoTracking().FirstAsync(x => x.OrderLineId == updated.OrderLineId);
        var parsed = discounts.TryParseDiscountSnapshot(row.UnitDiscountSnapshot);
        parsed!.Type.Should().Be("Amount");
        parsed.Value.Should().Be(5m);
    }

    

    [Fact]
    public async Task UpdateLine_SetIneligibleDiscount_Throws()
    {
        var (db, svc,discounts) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var item = Seed.Item(db, biz.BusinessId);
        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });
        var line = await svc.AddLineAsync(biz.BusinessId, order.OrderId, emp.EmployeeId,
            new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m });

        // discount not eligible for this item
        var disc = new Discount
        {
            BusinessId = biz.BusinessId, Code = "NOPE", Type = "Percent", Scope = "Line", Value = 12m,
            StartsAt = DateTime.UtcNow.AddDays(-1), EndsAt = DateTime.UtcNow.AddDays(10), Status = "Active"
        };
        db.Discounts.Add(disc);
        db.SaveChanges();

        await FluentActions.Invoking(() =>
            svc.UpdateLineAsync(biz.BusinessId, order.OrderId, line.OrderLineId, emp.EmployeeId,
                new UpdateLineRequest { DiscountId = disc.DiscountId }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*eligible*");
    }

    // ---------- Wrong business / not found / bad state ----------

    [Fact]
    public async Task AddLine_WrongBusiness_Throws()
    {
        var (db, svc,discounts) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var otherBiz = Seed.BizAndStaff(db).biz; // second business
        var item = Seed.Item(db, biz.BusinessId);

        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        await FluentActions.Invoking(() =>
            svc.AddLineAsync(otherBiz.BusinessId, order.OrderId, emp.EmployeeId,
                new AddLineRequest { CatalogItemId = item.CatalogItemId, Qty = 1m }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }
    

    [Fact]
    public async Task CancelOrder_Open_Ok_Closed_Throws()
    {
        var (db, svc, _) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);

        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        var cancelled = await svc.CancelOrderAsync(biz.BusinessId, order.OrderId, emp.EmployeeId, new CancelOrderRequest());
        cancelled.Status.Should().Be("Cancelled");

        await FluentActions.Invoking(() =>
            svc.CancelOrderAsync(biz.BusinessId, order.OrderId, emp.EmployeeId, new CancelOrderRequest()))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*only for OPEN*");
    }

    [Fact]
    public async Task RemoveLine_NotFound_Throws()
    {
        var (db, svc,discounts) = Boot();
        var (biz, emp) = Seed.BizAndStaff(db);
        var order = await svc.CreateOrderAsync(biz.BusinessId, emp.EmployeeId,
            new CreateOrderRequest { EmployeeId = emp.EmployeeId });

        await FluentActions.Invoking(() =>
            svc.RemoveLineAsync(biz.BusinessId, order.OrderId, orderLineId: 999, emp.EmployeeId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }
    
    
    [Fact]
    public async Task EnsureOrderDiscountEligible_Fails_WhenInactiveOrOutOfWindow()
    {
        await using var db = TestDb.NewInMemory();
        var svc = new DiscountsService(db);
        var (biz, _) = Seed.BizAndStaff(db);

        // inactive
        db.Discounts.Add(new PsP.Models.Discount
        {
            BusinessId = biz.BusinessId, Code = "ORD", Type = "Percent", Scope = "Order", Value = 5m,
            StartsAt = DateTime.UtcNow.AddDays(-10), EndsAt = DateTime.UtcNow.AddDays(10), Status = "Inactive"
        });
        await db.SaveChangesAsync();

        await FluentActions.Invoking(() =>
            svc.EnsureOrderDiscountEligibleAsync(biz.BusinessId, discountId: 1))
            .Should().ThrowAsync<InvalidOperationException>();

        // out of window
        db.Discounts.Add(new PsP.Models.Discount
        {
            BusinessId = biz.BusinessId, Code = "ORD2", Type = "Percent", Scope = "Order", Value = 5m,
            StartsAt = DateTime.UtcNow.AddDays(2), EndsAt = DateTime.UtcNow.AddDays(3), Status = "Active"
        });
        await db.SaveChangesAsync();

        await FluentActions.Invoking(() =>
            svc.EnsureOrderDiscountEligibleAsync(biz.BusinessId, discountId: 2))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EnsureLineDiscountEligible_Fails_WhenItemMissing_OrNotInEligibility()
    {
        await using var db = TestDb.NewInMemory();
        var svc = new DiscountsService(db);
        var (biz, _) = Seed.BizAndStaff(db);
        var item = Seed.Item(db, biz.BusinessId);

        var disc = new PsP.Models.Discount
        {
            BusinessId = biz.BusinessId, Code = "LINE", Type = "Percent", Scope = "Line", Value = 10m,
            StartsAt = DateTime.UtcNow.AddDays(-1), EndsAt = DateTime.UtcNow.AddDays(10), Status = "Active"
        };
        db.Discounts.Add(disc);
        await db.SaveChangesAsync();

        // item not found in business
        await FluentActions.Invoking(() =>
            svc.EnsureLineDiscountEligibleAsync(biz.BusinessId, disc.DiscountId, catalogItemId: 999))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Catalog item not found*");

        // still not eligible (no eligibility row)
        await FluentActions.Invoking(() =>
            svc.EnsureLineDiscountEligibleAsync(biz.BusinessId, disc.DiscountId, item.CatalogItemId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not eligible*");
    }

    [Fact]
    public void Snapshot_Writers_And_Parser_RoundTrip()
    {
        using var db = TestDb.NewInMemory();
        var svc = new DiscountsService(db);

        var now = DateTime.UtcNow;
        var d = new PsP.Models.Discount
        {
            DiscountId = 123, Code = "X", Type = "Percent", Scope = "Order",
            Value = 7.5m, StartsAt = now.AddDays(-1), EndsAt = now.AddDays(1), Status = "Active", BusinessId = 1
        };

        var json = svc.MakeOrderDiscountSnapshot(d, now);
        var parsed = svc.TryParseDiscountSnapshot(json);

        parsed.Should().NotBeNull();
        parsed!.Version.Should().Be(1);
        parsed.DiscountId.Should().Be(123);
        parsed.Scope.Should().Be("Order");
        parsed.Type.Should().Be("Percent");
        parsed.Value.Should().Be(7.5m);
        parsed.CapturedAtUtc.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));

        var jsonLine = svc.MakeLineDiscountSnapshot(d, catalogItemId: 55, now);
        var parsedLine = svc.TryParseDiscountSnapshot(jsonLine)!;
        parsedLine.CatalogItemId.Should().Be(55);
    }
    
    
    [Fact]
    public async Task Manager_Can_Reopen_Closed_Order()
    {
        var (db, svc,_) = Boot();
        var (biz, staff) = Seed.BizAndStaff(db);
        var mgr = Seed.AddEmployee(db, biz.BusinessId, "Manager");

        var order = await svc.CreateOrderAsync(biz.BusinessId, staff.EmployeeId,
            new CreateOrderRequest { EmployeeId = staff.EmployeeId });

        await svc.CancelOrderAsync(biz.BusinessId, order.OrderId, staff.EmployeeId,new CancelOrderRequest(){EmployeeId = staff.EmployeeId,Reason = "whatever"});

        var reopened = await svc.ReopenOrderAsync(biz.BusinessId, order.OrderId, mgr.EmployeeId);

        reopened.Status.Should().Be("Open");
        reopened.ClosedAt.Should().BeNull();
    }

    
    
    
    
    
    
}