
using Microsoft.Extensions.Logging;

namespace trackerApi.IntegrationTests.Fixtures;

// DatabaseFixture.cs
public class DatabaseFixture : IAsyncLifetime
{
    private readonly ILogger<AppDbContext> _logger;

    public AppDbContext Context { get; private set; }

    public DatabaseFixture(ILogger<AppDbContext> logger)
    {
        _logger = logger;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Testing.json")
            .AddUserSecrets<AppDbContextFactory>()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var dbPassword = configuration["DbPassword"];
        connectionString = connectionString!.Replace("${DbPassword}", dbPassword);

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        Context = new AppDbContext(optionsBuilder.Options, _logger);
    }

    public async Task InitializeAsync()
    {
        await Context.Database.EnsureCreatedAsync();
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanupTestDataAsync();
        await Context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Add any test data needed for all tests
        if (!await Context.Users.AnyAsync())
        {
            Context.Users.Add(new User
            {
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("testpassword")
            });
            await Context.SaveChangesAsync();
        }
    }

    private async Task CleanupTestDataAsync()
    {
        await Context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Users\", \"TrackingLogs\" RESTART IDENTITY CASCADE");
    }
}

// Define the collection for all database tests
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
