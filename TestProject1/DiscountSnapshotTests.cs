using PsP.Models;
using PsP.Services.Implementations;

namespace TestProject1;

public class DiscountSnapshotTests
{
       private static DiscountsService NewSvc()
        => new DiscountsService(TestHelpers.NewInMemoryContext());

    [Fact]
    public void MakeOrderDiscountSnapshot_RoundTrips()
    {
        // arrange
        var svc = NewSvc();
        var d = new Discount
        {
            DiscountId = 42,
            BusinessId = 1,
            Code       = "WELCOME10",
            Type       = "Percent",
            Scope      = "Order",
            Value      = 10.0m,
            StartsAt   = DateTime.UtcNow.AddDays(-1),
            EndsAt     = DateTime.UtcNow.AddDays(7),
            Status     = "Active"
        };
        var capturedAt = DateTime.UtcNow;

        // act
        var json = svc.MakeOrderDiscountSnapshot(d, capturedAtUtc: capturedAt);

        // assert (basic shape: camelCase)
        Assert.Contains("\"discountId\":", json);
        Assert.Contains("\"scope\":\"Order\"", json);
        Assert.Contains("\"type\":\"Percent\"", json);
        Assert.Contains("\"value\":10", json);

        // parse and verify fields
        var parsed = svc.TryParseDiscountSnapshot(json);
        Assert.NotNull(parsed);
        Assert.Equal(1, parsed!.Version);
        Assert.Equal(d.DiscountId, parsed.DiscountId);
        Assert.Equal(d.Code,       parsed.Code);
        Assert.Equal(d.Type,       parsed.Type);
        Assert.Equal("Order",      parsed.Scope);
        Assert.Null(parsed.CatalogItemId);
        Assert.Equal(d.Value,      parsed.Value);
        AssertClose(parsed.ValidFrom, d.StartsAt);
        AssertClose(parsed.ValidTo,   d.EndsAt);
        AssertClose(parsed.CapturedAtUtc, capturedAt);
    }

    [Fact]
    public void MakeLineDiscountSnapshot_RoundTrips()
    {
        // arrange
        var svc = NewSvc();
        var d = new Discount
        {
            DiscountId = 7,
            BusinessId = 1,
            Code       = "LATTE2OFF",
            Type       = "Amount",
            Scope      = "Line",
            Value      = 2.00m,
            StartsAt   = DateTime.UtcNow.AddHours(-2),
            EndsAt     = DateTime.UtcNow.AddDays(2),
            Status     = "Active"
        };
        var catalogItemId = 1234;
        var capturedAt = DateTime.UtcNow;

        // act
        var json = svc.MakeLineDiscountSnapshot(d, catalogItemId, capturedAt);

        // assert shape
        Assert.Contains("\"discountId\":", json);
        Assert.Contains("\"scope\":\"Line\"", json);
        Assert.Contains("\"type\":\"Amount\"", json);
        Assert.Contains("\"catalogItemId\":", json);

        // parse and verify fields
        var parsed = svc.TryParseDiscountSnapshot(json);
        Assert.NotNull(parsed);
        Assert.Equal(1, parsed!.Version);
        Assert.Equal(d.DiscountId, parsed.DiscountId);
        Assert.Equal(d.Code,       parsed.Code);
        Assert.Equal("Line",       parsed.Scope);
        Assert.Equal(catalogItemId, parsed.CatalogItemId);
        Assert.Equal(d.Value,      parsed.Value);
        AssertClose(parsed.ValidFrom, d.StartsAt);
        AssertClose(parsed.ValidTo,   d.EndsAt);
        AssertClose(parsed.CapturedAtUtc, capturedAt);
    }

    [Fact]
    public void TryParseDiscountSnapshot_ReturnsNull_OnNullOrEmpty()
    {
        var svc = NewSvc();
        Assert.Null(svc.TryParseDiscountSnapshot(null));
        Assert.Null(svc.TryParseDiscountSnapshot(""));
        Assert.Null(svc.TryParseDiscountSnapshot("   "));
    }

    [Fact]
    public void TryParseDiscountSnapshot_ReturnsNull_OnInvalidJson()
    {
        var svc = NewSvc();
        var badJson = "{ not: valid json }";
        Assert.Null(svc.TryParseDiscountSnapshot(badJson));
    }

    [Fact]
    public void Snapshots_UseCamelCase_Naming()
    {
        var svc = NewSvc();
        var d = new Discount
        {
            DiscountId = 1,
            BusinessId = 1,
            Code = "X",
            Type = "Percent",
            Scope = "Order",
            Value = 5m,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            EndsAt = DateTime.UtcNow.AddDays(1),
            Status = "Active"
        };

        var json = svc.MakeOrderDiscountSnapshot(d);
        // spot-check a few camelCase names
        Assert.Contains("\"discountId\":", json);
        Assert.Contains("\"capturedAtUtc\":", json);
        Assert.Contains("\"validFrom\":", json);
    }

    private static void AssertClose(DateTime actual, DateTime expected, double secondsTolerance = 1.5)
    {
        // JSON round-trip can slightly shift ticks; allow a small tolerance
        var delta = (actual.ToUniversalTime() - expected.ToUniversalTime()).Duration();
        Assert.True(delta.TotalSeconds <= secondsTolerance,
            $"Timestamps differ by {delta.TotalSeconds:F3}s; expected <= {secondsTolerance}s");
    }
}