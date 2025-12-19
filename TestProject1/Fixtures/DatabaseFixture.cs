using Microsoft.EntityFrameworkCore;
using PsP.Data;
using Xunit;

namespace TestProject1.Fixtures;

public class DatabaseFixture : IAsyncLifetime
{
    public AppDbContext Db { get; private set; } = null!;

    // IMPORTANT: use a dedicated TEST database
    private const string Conn =
        "Host=localhost;Port=5432;Database=pspdb_test;Username=postgres;Password=postgres";

    public async Task InitializeAsync()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Conn)
            .EnableSensitiveDataLogging()
            .Options;

        Db = new AppDbContext(opts);

        // Ensure a clean schema for each test run
        await Db.Database.EnsureDeletedAsync();
        await Db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Db.DisposeAsync();
    }
}

[CollectionDefinition("db")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }