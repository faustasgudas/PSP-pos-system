using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsP.Models;
using PsP.Services.Implementations;
using TestProject1;   // TestHelpers
using Xunit;

namespace PsP.Tests;

public class GiftCardServiceTests
{
    private PsP.Data.AppDbContext NewDb() => TestHelpers.NewInMemoryContext();

    private GiftCardService CreateService(PsP.Data.AppDbContext db) => new GiftCardService(db);

    [Fact]
    public async Task CreateAsync_SetsIssuedAt_And_DefaultStatusActive()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var card = new GiftCard
        {
            BusinessId = 1,
            Code = "CARD1",
            Balance = 1000
            // ExpiresAt = null, Status paliekam null – turi būti Active
        };

        var created = await service.CreateAsync(card);

        Assert.True(created.GiftCardId > 0);
        Assert.Equal("Active", created.Status);
        Assert.NotEqual(default, created.IssuedAt);

        var fromDb = await db.GiftCards.FirstAsync();
        Assert.Equal("CARD1", fromDb.Code);
        Assert.Equal("Active", fromDb.Status);
    }

    [Fact]
    public async Task TopUpAsync_Throws_WhenAmountNonPositive()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.TopUpAsync(1, 0)
        );
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.TopUpAsync(1, -10)
        );
    }

    [Fact]
    public async Task TopUpAsync_ReturnsFalse_WhenCardNotFound()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var ok = await service.TopUpAsync(999, 100);

        Assert.False(ok);
    }

    [Fact]
    public async Task TopUpAsync_IncreasesBalance_WhenActiveAndNotExpired()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var card = await service.CreateAsync(new GiftCard
        {
            BusinessId = 1,
            Code = "CARD2",
            Balance = 500,
            Status = "Active"
        });

        var result = await service.TopUpAsync(card.GiftCardId, 300);

        Assert.True(result);
        var fromDb = await db.GiftCards.FindAsync(card.GiftCardId);
        Assert.Equal(800, fromDb!.Balance);
    }

    [Fact]
    public async Task RedeemAsync_Throws_WhenAmountNonPositive()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.RedeemAsync(1, 0)
        );
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.RedeemAsync(1, -50)
        );
    }

    [Fact]
    public async Task RedeemAsync_Throws_NotFound_WhenCardMissing()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.RedeemAsync(999, 100)
        );
    }

    [Fact]
    public async Task RedeemAsync_Throws_WhenBlocked()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var card = await service.CreateAsync(new GiftCard
        {
            BusinessId = 1,
            Code = "BLOCKED",
            Balance = 500,
            Status = "Inactive"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RedeemAsync(card.GiftCardId, 100)
        );
    }

    [Fact]
    public async Task RedeemAsync_Throws_WhenExpired()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var card = await service.CreateAsync(new GiftCard
        {
            BusinessId = 1,
            Code = "EXPIRED",
            Balance = 500,
            Status = "Active",
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RedeemAsync(card.GiftCardId, 100)
        );
    }

    [Fact]
    public async Task RedeemAsync_ReturnsZero_WhenBalanceZero()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var card = await service.CreateAsync(new GiftCard
        {
            BusinessId = 1,
            Code = "ZERO",
            Balance = 0,
            Status = "Active"
        });

        var (charged, remaining) = await service.RedeemAsync(card.GiftCardId, 100);

        Assert.Equal(0, charged);
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task RedeemAsync_ChargesMinOfBalanceAndAmount()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var card = await service.CreateAsync(new GiftCard
        {
            BusinessId = 1,
            Code = "CARD3",
            Balance = 150,
            Status = "Active"
        });

        var (charged, remaining) = await service.RedeemAsync(card.GiftCardId, 200);

        Assert.Equal(150, charged);
        Assert.Equal(0, remaining);

        var fromDb = await db.GiftCards.FindAsync(card.GiftCardId);
        Assert.Equal(0, fromDb!.Balance);
    }

    [Fact]
    public async Task DeactivateAsync_ReturnsFalse_WhenNotFound()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var ok = await service.DeactivateAsync(999);

        Assert.False(ok);
    }

    [Fact]
    public async Task DeactivateAsync_SetsStatusInactive_WhenActive()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var card = await service.CreateAsync(new GiftCard
        {
            BusinessId = 1,
            Code = "CARD4",
            Balance = 100,
            Status = "Active"
        });

        var ok = await service.DeactivateAsync(card.GiftCardId);

        Assert.True(ok);
        var fromDb = await db.GiftCards.FindAsync(card.GiftCardId);
        Assert.Equal("Inactive", fromDb!.Status);
    }

    [Fact]
    public async Task DeactivateAsync_IsIdempotent_WhenAlreadyInactive()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var card = await service.CreateAsync(new GiftCard
        {
            BusinessId = 1,
            Code = "CARD5",
            Balance = 100,
            Status = "Inactive"
        });

        var ok = await service.DeactivateAsync(card.GiftCardId);

        Assert.True(ok);
        var fromDb = await db.GiftCards.FindAsync(card.GiftCardId);
        Assert.Equal("Inactive", fromDb!.Status);
    }
}
