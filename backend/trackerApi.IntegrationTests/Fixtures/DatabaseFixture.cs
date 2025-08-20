// DatabaseFixture.cs

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using dotenv.net;

namespace trackerApi.IntegrationTests.Fixtures;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly ILogger<AppDbContext> _logger;

    public AppDbContext Context { get; private set; }

    public DatabaseFixture()
    {
        // Setup logger
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("trackerApi", LogLevel.Debug)
                .AddConsole();
        });

        _logger = loggerFactory.CreateLogger<AppDbContext>();

        // Load .env file
        var currentDirectory = Directory.GetCurrentDirectory();
        var solutionDirectory = Directory.GetParent(currentDirectory)?.Parent?.Parent?.Parent?.FullName;
        var envPath = Path.Combine(solutionDirectory!, "trackerApi", ".env");

        if (!File.Exists(envPath))
        {
            throw new FileNotFoundException($"Could not find .env file at {envPath}");
        }

        DotEnv.Load(new DotEnvOptions(envFilePaths: new[] { envPath }));

        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Testing.json")
            .AddUserSecrets<AppDbContextFactory>()
            .AddEnvironmentVariables() // This will now pick up the .env values we just loaded
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        _logger.LogInformation("Initial connection string: {ConnectionString}", connectionString);

        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ??
                        configuration["DbPassword"];

        if (string.IsNullOrEmpty(dbPassword))
        {
            throw new InvalidOperationException(
                $"Database password not found. Checked environment variable 'DB_PASSWORD' and configuration key 'DbPassword'. " +
                $"Env file path: {envPath}");
        }

        _logger.LogInformation("Connection string template: {ConnectionString}", connectionString);
        
        connectionString = connectionString!.Replace("${DbPassword}", dbPassword);

        _logger.LogInformation("Using database configuration from: {ConfigPath}", Directory.GetCurrentDirectory());

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

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
                PasswordHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("testpassword")))
            });
            await Context.SaveChangesAsync();
        }
    }

    private async Task CleanupTestDataAsync()
    {
        // Use SQL Server syntax instead of PostgreSQL
        await Context.Database.ExecuteSqlRawAsync("DELETE FROM TrackingLog");
        await Context.Database.ExecuteSqlRawAsync("DELETE FROM Users WHERE Username = 'testuser'");
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
