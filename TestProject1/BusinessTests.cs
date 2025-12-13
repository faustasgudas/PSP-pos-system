using System.Threading.Tasks;
using PsP.Data;
using PsP.Models;
using PsP.Services.Implementations;
using TestProject1;        
using Xunit;
namespace PsP.Tests;
public class BusinessTests
{
    private AppDbContext NewDb() => TestHelpers.NewInMemoryContext();

    private BusinessService CreateService(AppDbContext db) => new BusinessService(db);

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoBusinesses()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var result = await service.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateAsync_CreatesBusiness()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var b = new Business
        {
            Name = "Test",
            Address = "A",
            Phone = "P",
            Email = "t@test.lt",
            BusinessType = "Catering",
            CountryCode = "LT"
        };

        var created = await service.CreateAsync(b);

        Assert.True(created.BusinessId > 0);
        var list = await service.GetAllAsync();
        Assert.Single(list);
        Assert.Equal("Test", list[0].Name);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var b = new Business
        {
            Name = "Old",
            Address = "A",
            Phone = "P",
            Email = "old@test.lt",
            BusinessType = "Catering",
            CountryCode = "LT"
        };

        db.Businesses.Add(b);
        await db.SaveChangesAsync();

        var updated = new Business
        {
            Name = "New",
            Address = "NewAddress",
            Phone = "222",
            Email = "new@test.lt",
            BusinessType = "Catering",
            CountryCode = "LV",
            PriceIncludesTax = true
        };

        var result = await service.UpdateAsync(b.BusinessId, updated);

        Assert.NotNull(result);
        Assert.Equal("New", result!.Name);
        Assert.Equal("NewAddress", result.Address);
    }

    [Fact]
    public async Task DeleteAsync_RemovesBusiness()
    {
        await using var db = NewDb();
        var service = CreateService(db);

        var b = new Business
        {
            Name = "DeleteMe",
            Address = "A",
            Phone = "P",
            Email = "d@test.lt",
            BusinessType = "Catering",
            CountryCode = "LT"
        };

        db.Businesses.Add(b);
        await db.SaveChangesAsync();

        var removed = await service.DeleteAsync(b.BusinessId);

        Assert.True(removed);

        var all = await service.GetAllAsync();
        Assert.Empty(all);
    }
}
