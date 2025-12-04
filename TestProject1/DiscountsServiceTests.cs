using PsP.Data;
using PsP.Models;
using PsP.Services.Implementations;

namespace TestProject1;

public class DiscountsServiceTests
{
    private static DiscountsService NewSvc(AppDbContext db) => new DiscountsService(db);

    [Fact]
    public async Task GetNewestOrderDiscountAsync_ReturnsLatestActiveWithinWindow()
    {
        await using var db = TestHelpers.NewContext();
        var (biz, _) = TestHelpers.SeedBusinessAndEmployee(db);

        // older
        db.Discounts.Add(new Discount {
            BusinessId = biz.BusinessId, Code = "ORD10-OLD",
            Type = "Percent", Scope = "Order", Value = 10m,
            StartsAt = DateTime.UtcNow.AddDays(-10), EndsAt = DateTime.UtcNow.AddDays( -1 ),
            Status = "Active"
        });
        // active but older start
        db.Discounts.Add(new Discount {
            BusinessId = biz.BusinessId, Code = "ORD5",
            Type = "Amount", Scope = "Order", Value = 5m,
            StartsAt = DateTime.UtcNow.AddDays(-5), EndsAt = DateTime.UtcNow.AddDays(  5 ),
            Status = "Active"
        });
        // active with newer start (should win)
        db.Discounts.Add(new Discount {
            BusinessId = biz.BusinessId, Code = "ORD7-NEW",
            Type = "Amount", Scope = "Order", Value = 7m,
            StartsAt = DateTime.UtcNow.AddDays(-2), EndsAt = DateTime.UtcNow.AddDays(  5 ),
            Status = "Active"
        });
        await db.SaveChangesAsync();

        var svc = NewSvc(db);
        var d = await svc.GetNewestOrderDiscountAsync(biz.BusinessId);

        Assert.NotNull(d);
        Assert.Equal("ORD7-NEW", d!.Code);
    }

    [Fact]
    public async Task GetNewestLineDiscountForItemAsync_RespectsEligibilityAndWindow()
    {
        await using var db = TestHelpers.NewContext();
        var (biz, _) = TestHelpers.SeedBusinessAndEmployee(db);
        var itemA = TestHelpers.SeedCatalogItem(db, biz.BusinessId);
        var itemB = TestHelpers.SeedCatalogItem(db, biz.BusinessId);

        var good = new Discount {
            BusinessId = biz.BusinessId, Code = "LINE20",
            Type = "Percent", Scope = "Line", Value = 20m,
            StartsAt = DateTime.UtcNow.AddHours(-3), EndsAt = DateTime.UtcNow.AddHours(3),
            Status = "Active",
            Eligibilities = new List<DiscountEligibility> {
                new DiscountEligibility { CatalogItemId = itemA.CatalogItemId }
            }
        };
        var notEligibleForA = new Discount {
            BusinessId = biz.BusinessId, Code = "LINE15",
            Type = "Percent", Scope = "Line", Value = 15m,
            StartsAt = DateTime.UtcNow.AddHours(-3), EndsAt = DateTime.UtcNow.AddHours(3),
            Status = "Active",
            Eligibilities = new List<DiscountEligibility> {
                new DiscountEligibility { CatalogItemId = itemB.CatalogItemId }
            }
        };
        db.Discounts.AddRange(good, notEligibleForA);
        await db.SaveChangesAsync();

        var svc = NewSvc(db);
        var found = await svc.GetNewestLineDiscountForItemAsync(biz.BusinessId, itemA.CatalogItemId);

        Assert.NotNull(found);
        Assert.Equal("LINE20", found!.Code);
    }

    [Fact]
    public async Task EnsureLineDiscountEligibleAsync_Throws_WhenNotEligible()
    {
        await using var db = TestHelpers.NewContext();
        var (biz, _) = TestHelpers.SeedBusinessAndEmployee(db);
        var item = TestHelpers.SeedCatalogItem(db, biz.BusinessId);

        var d = new Discount {
            BusinessId = biz.BusinessId, Code = "LINE10",
            Type = "Percent", Scope = "Line", Value = 10m,
            StartsAt = DateTime.UtcNow.AddDays(-1), EndsAt = DateTime.UtcNow.AddDays(1),
            Status = "Active",
            Eligibilities = new List<DiscountEligibility>() // empty -> not eligible
        };
        db.Discounts.Add(d);
        await db.SaveChangesAsync();

        var svc = NewSvc(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.EnsureLineDiscountEligibleAsync(biz.BusinessId, d.DiscountId, item.CatalogItemId));
    }

    [Fact]
    public async Task EnsureOrderDiscountEligibleAsync_ReturnsDiscount_WhenOk()
    {
        await using var db = TestHelpers.NewContext();
        var (biz, _) = TestHelpers.SeedBusinessAndEmployee(db);

        var d = new Discount {
            BusinessId = biz.BusinessId, Code = "ORDOK",
            Type = "Amount", Scope = "Order", Value = 3.50m,
            StartsAt = DateTime.UtcNow.AddHours(-1), EndsAt = DateTime.UtcNow.AddHours(1),
            Status = "Active"
        };
        db.Discounts.Add(d);
        await db.SaveChangesAsync();

        var svc = NewSvc(db);
        var got = await svc.EnsureOrderDiscountEligibleAsync(biz.BusinessId, d.DiscountId);

        Assert.Equal("ORDOK", got.Code);
        Assert.Equal(3.50m, got.Value);
    }

    [Fact]
    public void Snapshot_Writers_And_TryParse_RoundTrip()
    {
        // minimal fake discount to serialize
        var d = new Discount {
            DiscountId = 42,
            Code = "LINE25",
            Type = "Percent",
            Scope = "Line",
            Value = 25m,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            EndsAt   = DateTime.UtcNow.AddDays( 1 ),
            Status = "Active",
        };
        var svc = NewSvc(TestHelpers.NewContext()); // context not used here

        var json = svc.MakeLineDiscountSnapshot(d, catalogItemId: 123);
        Assert.False(string.IsNullOrWhiteSpace(json));

        var snap = svc.TryParseDiscountSnapshot(json);
        Assert.NotNull(snap);
        Assert.Equal(1, snap!.Version);
        Assert.Equal(42, snap.DiscountId);
        Assert.Equal("LINE25", snap.Code);
        Assert.Equal("Percent", snap.Type);
        Assert.Equal("Line", snap.Scope);
        Assert.Equal(25m, snap.Value);
        Assert.Equal(123, snap.CatalogItemId);
        Assert.True(snap.CapturedAtUtc > DateTime.UtcNow.AddMinutes(-5));
    }
}