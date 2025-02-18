using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using trackerApi.DbContext;

namespace trackerApi.UnitTests;

// Unit Tests
public class AppDbContextUnitTests : IClassFixture<EnvironmentVariableFixture>
{
    private readonly IConfiguration _testConfiguration;

    public AppDbContextUnitTests(EnvironmentVariableFixture fixture)
    {
        //Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "unitTests");
        
        _testConfiguration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.unitTests.json", optional: false)
            .Build();
    }

    [Fact]
    public void CreateDbContext_ShouldCreateValidContext()
    {
        // Arrange
        var factory = new AppDbContextFactory(_testConfiguration);

        // Act
        var context = factory.CreateDbContext([]);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<AppDbContext>(context);
    }

    [Fact]
    public void CreateDbContext_ShouldHaveValidConnectionString()
    {
        // Arrange
        var factory = new AppDbContextFactory(_testConfiguration);

        // Act
        var context = factory.CreateDbContext([]);
        var connection = context.Database.GetDbConnection();

        // Assert
        Assert.NotNull(connection);
        Assert.Contains("Host=database-1", connection.ConnectionString);
        Assert.Contains("Database=BETrackingDb", connection.ConnectionString);
        Assert.DoesNotContain("${DbPassword}", connection.ConnectionString);
    }
}

public class EnvironmentVariableFixture : IDisposable
{
    private readonly string? _originalValue;
    private readonly string _variableName;

    public EnvironmentVariableFixture()
    {
        _variableName = "ASPNETCORE_ENVIRONMENT";
        _originalValue = Environment.GetEnvironmentVariable(_variableName);
        Environment.SetEnvironmentVariable(_variableName, "unitTests");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(_variableName, _originalValue);
    }
}
