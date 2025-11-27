using Microsoft.EntityFrameworkCore;
using PsP.Data;

namespace TestProject1.Fixtures;

public class DatabaseFixture : IAsyncLifetime
{
    public AppDbContext Db { get; private set; } = null!;

    // Same connection you use in your appsettings (Docker Postgres)
    private const string Conn =
        "Host=localhost;Port=5432;Database=pspdb;Username=postgres;Password=postgres";

    public async Task InitializeAsync()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Conn)
            .EnableSensitiveDataLogging()
            .Options;

        Db = new AppDbContext(opts);
        await Db.Database.MigrateAsync(); // applies your EF migrations
    }

    public async Task DisposeAsync() => await Db.DisposeAsync();
}

[CollectionDefinition("db")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }