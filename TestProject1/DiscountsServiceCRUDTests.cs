using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PsP.Contracts.Discounts;
using PsP.Data;
using PsP.Models;
using PsP.Services.Implementations;
using PsP.Services.Interfaces;

namespace TestProject1;

public class DiscountsServiceCRUDTests
{
    
    private static (Business biz, Employee owner, Employee manager, Employee staff) SeedOrg(AppDbContext db)
    {
        var biz = new Business
        {
            Name = "Biz-" + Guid.NewGuid().ToString("N")[..6],
            Address = "Any 1",
            Email = "b@b.lt",
            Phone = "+370000000",
            CountryCode = "LT",
            PriceIncludesTax = false,
            BusinessType = "Catering"
        };
        db.Businesses.Add(biz);
        db.SaveChanges();

        var owner = new Employee { BusinessId = biz.BusinessId, Name = "Ow", Role = "Owner", Status = "Active",Email = "a@b.c",PasswordHash = "whatever1" };
        var manager = new Employee { BusinessId = biz.BusinessId, Name = "Mgr", Role = "Manager", Status = "Active",Email = "b@b.c",PasswordHash = "whatever2" };
        var staff = new Employee { BusinessId = biz.BusinessId, Name = "Stf", Role = "Staff", Status = "Active",Email = "c@b.c",PasswordHash = "whatever3" };
        db.Employees.AddRange(owner, manager, staff);
        db.SaveChanges();

        return (biz, owner, manager, staff);
    }

    private static CatalogItem SeedItem(AppDbContext db, int bizId, string name = "Item")
    {
        var item = new CatalogItem
        {
            BusinessId = bizId,
            Name = name,
            Code = "SKU-" + Guid.NewGuid().ToString("N")[..6],
            Type = "Product",
            Status = "Active",
            TaxClass = "Food",
            BasePrice = 3.50m,
            DefaultDurationMin = 0
        };
        db.CatalogItems.Add(item);
        db.SaveChanges();
        return item;
    }

    private static (AppDbContext db, IDiscountsService svc, Business biz, Employee owner, Employee manager, Employee staff)
        Boot()
    {
        var db = TestHelpers.NewInMemoryContext();
        var svc = new DiscountsService(db);
        var (biz, owner, manager, staff) = SeedOrg(db);
        return (db, svc, biz, owner, manager, staff);
    }

    // --------- list/get ---------
    [Fact]
    public async Task ListDiscounts_AnyEmployee_Allowed()
    {
        var (db, svc, biz, _, _, staff) = Boot();

        // seed one
        db.Discounts.Add(new Discount
        {
            BusinessId = biz.BusinessId,
            Code = "SUMMER10",
            Type = "Percent",
            Scope = "Order",
            Value = 10m,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            EndsAt = DateTime.UtcNow.AddDays(3),
            Status = "Active"
        });
        await db.SaveChangesAsync();

        var list = await svc.ListDiscountsAsync(biz.BusinessId, staff.EmployeeId);
        list.Should().NotBeEmpty();
        list.All(d => d.BusinessId == biz.BusinessId).Should().BeTrue();
    }

    [Fact]
    public async Task GetDiscount_ReturnsDetail_Or_NotFound()
    {
        var (db, svc, biz, _, _, staff) = Boot();

        var d = new Discount
        {
            BusinessId = biz.BusinessId,
            Code = "ORD5",
            Type = "Amount",
            Scope = "Order",
            Value = 5m,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            EndsAt = DateTime.UtcNow.AddDays(10),
            Status = "Active"
        };
        db.Discounts.Add(d);
        await db.SaveChangesAsync();

        // ok
        var dto = await svc.GetDiscountAsync(biz.BusinessId, staff.EmployeeId, d.DiscountId);
        dto.DiscountId.Should().Be(d.DiscountId);
        dto.Eligibilities.Should().NotBeNull();

        // not found
        await FluentActions.Invoking(() => svc.GetDiscountAsync(biz.BusinessId, staff.EmployeeId, 9999))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // --------- create ---------
    [Fact]
    public async Task CreateDiscount_Staff_Forbidden()
    {
        var (db, svc, biz, _, _, staff) = Boot();

        var req = new CreateDiscountRequest
        {
            Code = "X",
            Type = "Percent",
            Scope = "Order",
            Value = 10m,
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(1),
            Status = "Active"
        };

        await FluentActions.Invoking(() => svc.CreateDiscountAsync(biz.BusinessId, staff.EmployeeId, req))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Forbidden*");
    }

    [Fact]
    public async Task CreateDiscount_Manager_Succeeds_AndPersists()
    {
        var (db, svc, biz, _, manager, _) = Boot();

        var req = new CreateDiscountRequest
        {
            Code = "HAPPY",
            Type = "Percent",
            Scope = "Line",
            Value = 12m,
            StartsAt = DateTime.UtcNow.AddHours(-1),
            EndsAt = DateTime.UtcNow.AddHours(10),
            Status = "Active"
        };

        var created = await svc.CreateDiscountAsync(biz.BusinessId, manager.EmployeeId, req);
        created.Code.Should().Be("HAPPY");

        var inDb = await db.Discounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DiscountId == created.DiscountId);
        inDb.Should().NotBeNull();
        inDb!.StartsAt.Should().BeBefore(inDb.EndsAt);
    }

    [Fact]
    public async Task CreateDiscount_Requires_Start_And_End_And_ValidWindow()
    {
        var (db, svc, biz, _, owner, _) = Boot();

        // End before Start
        var bad = new CreateDiscountRequest
        {
            Code = "BADWIN",
            Type = "Percent",
            Scope = "Order",
            Value = 5m,
            StartsAt = DateTime.UtcNow.AddDays(2),
            EndsAt = DateTime.UtcNow.AddDays(1),
            Status = "Active"
        };

        await FluentActions.Invoking(() => svc.CreateDiscountAsync(biz.BusinessId, owner.EmployeeId, bad))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*StartsAt must be before EndsAt*");

        // Missing dates cannot compile to null (DTO has non-nullable DateTime),
        // but just in case someone passes default/min values—reject:
        var minDates = new CreateDiscountRequest
        {
            Code = "MINDATES",
            Type = "Percent",
            Scope = "Order",
            Value = 5m,
            StartsAt = default,
            EndsAt = default,
            Status = "Active"
        };
        await FluentActions.Invoking(() => svc.CreateDiscountAsync(biz.BusinessId, owner.EmployeeId, minDates))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*StartsAt must be before EndsAt*");
    }

    [Fact]
    public async Task CreateDiscount_Enforces_Code_Uniqueness_PerBusiness()
    {
        var (db, svc, biz, _, owner, _) = Boot();

        var now = DateTime.UtcNow;
        db.Discounts.Add(new Discount
        {
            BusinessId = biz.BusinessId,
            Code = "CODEX",
            Type = "Percent",
            Scope = "Order",
            Value = 10m,
            StartsAt = now.AddHours(-1),
            EndsAt = now.AddHours(10),
            Status = "Active"
        });
        await db.SaveChangesAsync();

        var dup = new CreateDiscountRequest
        {
            Code = "codex", // different case, same normalized
            Type = "Percent",
            Scope = "Order",
            Value = 12m,
            StartsAt = now,
            EndsAt = now.AddDays(1),
            Status = "Active"
        };

        await FluentActions.Invoking(() => svc.CreateDiscountAsync(biz.BusinessId, owner.EmployeeId, dup))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    // --------- update ---------
    [Fact]
    public async Task UpdateDiscount_Staff_Forbidden()
    {
        var (db, svc, biz, _, _, staff) = Boot();
        var now = DateTime.UtcNow;

        var d = new Discount
        {
            BusinessId = biz.BusinessId,
            Code = "UP",
            Type = "Percent",
            Scope = "Order",
            Value = 10m,
            StartsAt = now.AddHours(-1),
            EndsAt = now.AddDays(1),
            Status = "Active"
        };
        db.Discounts.Add(d);
        await db.SaveChangesAsync();

        var body = new UpdateDiscountRequest { Value = 15m };
        await FluentActions.Invoking(() => svc.UpdateDiscountAsync(biz.BusinessId, staff.EmployeeId, d.DiscountId, body))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Forbidden*");
    }

    [Fact]
    public async Task UpdateDiscount_Manager_Can_Change_Fields_And_Enforce_CodeUnique()
    {
        var (db, svc, biz, _, manager, _) = Boot();
        var now = DateTime.UtcNow;

        var d1 = new Discount
        {
            BusinessId = biz.BusinessId,
            Code = "A",
            Type = "Percent",
            Scope = "Order",
            Value = 5m,
            StartsAt = now.AddHours(-1),
            EndsAt = now.AddDays(1),
            Status = "Active"
        };
        var d2 = new Discount
        {
            BusinessId = biz.BusinessId,
            Code = "B",
            Type = "Percent",
            Scope = "Order",
            Value = 7m,
            StartsAt = now.AddHours(-1),
            EndsAt = now.AddDays(1),
            Status = "Active"
        };
        db.Discounts.AddRange(d1, d2);
        await db.SaveChangesAsync();

        // change value ok
        var updated = await svc.UpdateDiscountAsync(biz.BusinessId, manager.EmployeeId, d1.DiscountId,
            new UpdateDiscountRequest { Value = 9m });
        updated.Value.Should().Be(9m);

        // change code to d2's code -> should fail
        await FluentActions.Invoking(() => svc.UpdateDiscountAsync(biz.BusinessId, manager.EmployeeId, d1.DiscountId,
                new UpdateDiscountRequest { Code = "b" }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    // --------- delete ---------
    [Fact]
    public async Task DeleteDiscount_Staff_Forbidden_Manager_Ok()
    {
        var (db, svc, biz, _, manager, staff) = Boot();
        var now = DateTime.UtcNow;

        var d = new Discount
        {
            BusinessId = biz.BusinessId,
            Code = "DEL",
            Type = "Amount",
            Scope = "Order",
            Value = 3m,
            StartsAt = now.AddHours(-1),
            EndsAt = now.AddDays(1),
            Status = "Active"
        };
        db.Discounts.Add(d);
        await db.SaveChangesAsync();

        await FluentActions.Invoking(() => svc.DeleteDiscountAsync(biz.BusinessId, staff.EmployeeId, d.DiscountId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Forbidden*");

        await svc.DeleteDiscountAsync(biz.BusinessId, manager.EmployeeId, d.DiscountId);
        (await db.Discounts.FindAsync(d.DiscountId)).Should().BeNull();
    }

    // --------- eligibilities (List/Add/Remove) ---------
    [Fact]
    public async Task ListEligibilities_Works_And_OnlyForDiscountBusiness()
    {
        var (db, svc, biz, _, _, staff) = Boot();
        var item = SeedItem(db, biz.BusinessId);

        var now = DateTime.UtcNow;
        var d = new Discount
        {
            BusinessId = biz.BusinessId,
            Code = "ELIG",
            Type = "Percent",
            Scope = "Line",
            Value = 10m,
            StartsAt = now.AddHours(-1),
            EndsAt = now.AddDays(1),
            Status = "Active",
        };
        db.Discounts.Add(d);
        await db.SaveChangesAsync();

        db.DiscountEligibilities.Add(new DiscountEligibility
        {
            DiscountId = d.DiscountId,
            CatalogItemId = item.CatalogItemId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var list = await svc.ListEligibilitiesAsync(biz.BusinessId, staff.EmployeeId, d.DiscountId);
        list.Should().HaveCount(1);
    }

    [Fact]
public async Task AddEligibility_ManagerOnly_PreventsDuplicates_And_BusinessMismatch()
{
    var (db, svc, biz, _, manager, staff) = Boot();
    var item = SeedItem(db, biz.BusinessId);
    var now  = DateTime.UtcNow;

    var d = new Discount
    {
        BusinessId = biz.BusinessId,
        Code = "EADD",
        Type = "Percent",
        Scope = "Line",
        Value = 5m,
        StartsAt = now.AddHours(-1),
        EndsAt   = now.AddDays(1),
        Status   = "Active"
    };
    db.Discounts.Add(d);
    await db.SaveChangesAsync();

    var body = new CreateDiscountEligibilityRequest { CatalogItemId = item.CatalogItemId };

    // staff forbidden
    await FluentActions.Invoking(() =>
            svc.AddEligibilityAsync(biz.BusinessId, staff.EmployeeId, d.DiscountId, body))
        .Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("Forbidden*");

    // manager ok first time
    var row = await svc.AddEligibilityAsync(biz.BusinessId, manager.EmployeeId, d.DiscountId, body);
    row.CatalogItemId.Should().Be(item.CatalogItemId);

    // duplicate
    await FluentActions.Invoking(() =>
            svc.AddEligibilityAsync(biz.BusinessId, manager.EmployeeId, d.DiscountId, body))
        .Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*already exists*");

    // --- BUSINESS MISMATCH (create another business IN THE SAME db) ---
    var otherBiz = new Business
    {
        Name = "OtherBiz",
        Address = "X",
        Email = "x@x.lt",
        Phone = "+370",
        CountryCode = "LT",
        BusinessType = "Catering",
        PriceIncludesTax = false
    };
    db.Businesses.Add(otherBiz);
    await db.SaveChangesAsync();

    var otherItem = SeedItem(db, otherBiz.BusinessId, "OtherItem"); // different business in SAME db
    var bad = new CreateDiscountEligibilityRequest { CatalogItemId = otherItem.CatalogItemId };

    await FluentActions.Invoking(() =>
            svc.AddEligibilityAsync(biz.BusinessId, manager.EmployeeId, d.DiscountId, bad))
        .Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*Catalog item not found*");
}

    [Fact]
    public async Task RemoveEligibility_ManagerOnly_Idempotent()
    {
        var (db, svc, biz, _, manager, staff) = Boot();
        var item = SeedItem(db, biz.BusinessId);
        var now = DateTime.UtcNow;

        var d = new Discount
        {
            BusinessId = biz.BusinessId,
            Code = "EREM",
            Type = "Percent",
            Scope = "Line",
            Value = 11m,
            StartsAt = now.AddHours(-1),
            EndsAt = now.AddDays(1),
            Status = "Active"
        };
        db.Discounts.Add(d);
        await db.SaveChangesAsync();

        db.DiscountEligibilities.Add(new DiscountEligibility
        {
            DiscountId = d.DiscountId,
            CatalogItemId = item.CatalogItemId
        });
        await db.SaveChangesAsync();

        // staff forbidden
        await FluentActions.Invoking(() => svc.RemoveEligibilityAsync(biz.BusinessId, staff.EmployeeId, d.DiscountId, item.CatalogItemId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Forbidden*");

        // manager ok
        await svc.RemoveEligibilityAsync(biz.BusinessId, manager.EmployeeId, d.DiscountId, item.CatalogItemId);
        var still = await db.DiscountEligibilities
            .FirstOrDefaultAsync(e => e.DiscountId == d.DiscountId && e.CatalogItemId == item.CatalogItemId);
        still.Should().BeNull();

        // idempotent: removing again should not throw
        await svc.RemoveEligibilityAsync(biz.BusinessId, manager.EmployeeId, d.DiscountId, item.CatalogItemId);
    }

    // --------- “engine” helpers on service (newest/ensure) ---------
    [Fact]
    public async Task GetNewestLineDiscountForItem_Picks_Latest_Active_By_StartsAt()
    {
        var (db, svc, biz, _, owner, _) = Boot();
        var item = SeedItem(db, biz.BusinessId);
        var now = DateTime.UtcNow;

        var dOld = new Discount
        {
            BusinessId = biz.BusinessId, Code = "L1", Type = "Percent", Scope = "Line", Value = 5m,
            StartsAt = now.AddDays(-5), EndsAt = now.AddDays(5), Status = "Active"
        };
        var dNew = new Discount
        {
            BusinessId = biz.BusinessId, Code = "L2", Type = "Percent", Scope = "Line", Value = 7m,
            StartsAt = now.AddDays(-1), EndsAt = now.AddDays(5), Status = "Active"
        };
        db.Discounts.AddRange(dOld, dNew);
        await db.SaveChangesAsync();

        // link only dNew to the item
        db.DiscountEligibilities.Add(new DiscountEligibility { DiscountId = dNew.DiscountId, CatalogItemId = item.CatalogItemId });
        await db.SaveChangesAsync();

        var d = await svc.GetNewestLineDiscountForItemAsync(biz.BusinessId, item.CatalogItemId);
        d.Should().NotBeNull();
        d!.Code.Should().Be("L2");
    }

    [Fact]
    public async Task EnsureOrderDiscountEligible_Respects_DateWindow_And_Scope()
    {
        var (db, svc, biz, _, owner, _) = Boot();
        var now = DateTime.UtcNow;

        var inactive = new Discount
        {
            BusinessId = biz.BusinessId, Code = "X", Type = "Percent", Scope = "Order", Value = 5m,
            StartsAt = now.AddDays(-5), EndsAt = now.AddDays(-1), Status = "Active"
        };
        var active = new Discount
        {
            BusinessId = biz.BusinessId, Code = "Y", Type = "Percent", Scope = "Order", Value = 10m,
            StartsAt = now.AddDays(-1), EndsAt = now.AddDays(5), Status = "Active"
        };
        db.Discounts.AddRange(inactive, active);
        await db.SaveChangesAsync();

        await FluentActions.Invoking(() => svc.EnsureOrderDiscountEligibleAsync(biz.BusinessId, inactive.DiscountId))
            .Should().ThrowAsync<InvalidOperationException>();

        var ok = await svc.EnsureOrderDiscountEligibleAsync(biz.BusinessId, active.DiscountId);
        ok.DiscountId.Should().Be(active.DiscountId);
    }
    
    
    [Fact]
        public async Task GetOrderDiscountAsync_Finds_By_Id()
        {
            var (db, svc, biz, _,_,_) = Boot();

            var d = new Discount
            {
                BusinessId = biz.BusinessId,
                Code = "ORDER10",
                Type = "Percent",
                Scope = "Order",
                Value = 10m,
                StartsAt = DateTime.UtcNow.AddDays(-1),
                EndsAt = DateTime.UtcNow.AddDays(10),
                Status = "Active"
            };
            db.Discounts.Add(d);
            await db.SaveChangesAsync();
            
            

            var found = await svc.GetOrderDiscountAsync(d.DiscountId);

            found.Should().NotBeNull();
            found!.DiscountId.Should().Be(d.DiscountId);
            found.Scope.Should().Be("Order");
            found.Code.Should().Be("ORDER10");
        }

        [Fact]
        public async Task GetOrderDiscountAsync_ReturnsNull_When_NotFound()
        {
            var (_, svc, _, _,_,_) = Boot();
            var found = await svc.GetOrderDiscountAsync(999_999);
            found.Should().BeNull();
        }

        // -------- ListEligibleItemsAsync --------

        [Fact]
        public async Task ListEligibleItemsAsync_Returns_Joined_CatalogItems_For_Discount()
        {
            var (db, svc, biz, _,_,emp) = Boot();

            // two catalog items
            var itemA = new CatalogItem
            {
                BusinessId = biz.BusinessId,
                Name = "Alpha Cut",
                Code = "A1",
                Type = "Service",
                BasePrice = 25m,
                Status = "Active",
                DefaultDurationMin = 30,
                TaxClass = "Service"
            };
            var itemB = new CatalogItem
            {
                BusinessId = biz.BusinessId,
                Name = "Beard Trim",
                Code = "B1",
                Type = "Service",
                BasePrice = 15m,
                Status = "Active",
                DefaultDurationMin = 15,
                TaxClass = "Service"
            };
            db.CatalogItems.AddRange(itemA, itemB);
            await db.SaveChangesAsync();

            var d = new Discount
            {
                BusinessId = biz.BusinessId,
                Code = "LINE5",
                Type = "Amount",
                Scope = "Line",
                Value = 5m,
                StartsAt = DateTime.UtcNow.AddDays(-1),
                EndsAt = DateTime.UtcNow.AddDays(10),
                Status = "Active"
            };
            db.Discounts.Add(d);
            await db.SaveChangesAsync();

            // add eligibilities for both items
            db.DiscountEligibilities.AddRange(
                new DiscountEligibility { DiscountId = d.DiscountId, CatalogItemId = itemA.CatalogItemId },
                new DiscountEligibility { DiscountId = d.DiscountId, CatalogItemId = itemB.CatalogItemId }
            );
            await db.SaveChangesAsync();

            var list = (await svc.ListEligibleItemsAsync(
                biz.BusinessId, emp.EmployeeId, d.DiscountId)).ToList();

            list.Should().HaveCount(2);
            // ordered by Name in service; check order and fields
            list[0].CatalogItemId.Should().Be(itemA.CatalogItemId);
            list[0].Name.Should().Be("Alpha Cut");
            list[1].CatalogItemId.Should().Be(itemB.CatalogItemId);
            list[1].Name.Should().Be("Beard Trim");
        }

        [Fact]
        public async Task ListEligibleItemsAsync_Throws_When_Discount_Not_In_Business()
        {
            var (db, svc, biz, _,_,emp) = Boot();
            // discount in ANOTHER business
            var otherBiz = new Business
            {
                Name = "Other",
                Address = "B st. 2",
                Phone = "+370000001",
                Email = "o@test",
                CountryCode = "LT",
                BusinessType = "Catering",
                PriceIncludesTax = false
            };
            db.Businesses.Add(otherBiz);
            await db.SaveChangesAsync();

            var d = new Discount
            {
                BusinessId = otherBiz.BusinessId,
                Code = "OTHER",
                Type = "Percent",
                Scope = "Line",
                Value = 5m,
                StartsAt = DateTime.UtcNow.AddDays(-1),
                EndsAt = DateTime.UtcNow.AddDays(1),
                Status = "Active"
            };
            db.Discounts.Add(d);
            await db.SaveChangesAsync();

            var act = async () => await svc.ListEligibleItemsAsync(
                biz.BusinessId, emp.EmployeeId, d.DiscountId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Discount not found*");
        }

        [Fact]
        public async Task ListEligibleItemsAsync_Throws_When_Caller_Not_In_Business()
        {
            var (db, svc, biz, _,_,_) = Boot();

            // discount belongs to biz
            var d = new Discount
            {
                BusinessId = biz.BusinessId,
                Code = "LINEX",
                Type = "Percent",
                Scope = "Line",
                Value = 7m,
                StartsAt = DateTime.UtcNow.AddDays(-1),
                EndsAt = DateTime.UtcNow.AddDays(1),
                Status = "Active"
            };
            db.Discounts.Add(d);
            await db.SaveChangesAsync();

            // caller from different business
            var otherBiz = new Business
            {
                Name = "Other",
                Address = "C st. 3",
                Phone = "+370000002",
                Email = "o2@test",
                CountryCode = "LT",
                BusinessType = "Catering",
                PriceIncludesTax = false
            };
            db.Businesses.Add(otherBiz);
            await db.SaveChangesAsync();

            var otherEmp = new Employee
            {
                BusinessId = otherBiz.BusinessId,
                Name = "Bob",
                Role = "Staff",
                Status = "Active",
                Email = "d@b.c",PasswordHash = "1whatever"
            };
            db.Employees.Add(otherEmp);
            await db.SaveChangesAsync();

            var act = async () => await svc.ListEligibleItemsAsync(
                biz.BusinessId, otherEmp.EmployeeId, d.DiscountId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Caller employee not found*");
        }
    }
    
    
    
    
