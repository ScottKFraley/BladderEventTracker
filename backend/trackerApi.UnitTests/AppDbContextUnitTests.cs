using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using trackerApi.DbContext;
using trackerApi.EndPoints;
using trackerApi.Services;

namespace trackerApi.UnitTests;

// Unit Tests
public class AppDbContextUnitTests : IClassFixture<EnvironmentVariableFixture>
{
    private readonly IConfiguration _testConfiguration;
    private readonly Mock<ILogger<AppDbContext>> mockDbCtxLogger;

    public AppDbContextUnitTests(EnvironmentVariableFixture fixture)
    {
        // TODO: Why aren't I doing this?
        //Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "unitTests");
        
        _testConfiguration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.unitTests.json", optional: false)
            .Build();

        mockDbCtxLogger = new Mock<ILogger<AppDbContext>>();
    }

    [Fact]
    public void CreateDbContext_ShouldCreateValidContext()
    {
        // Arrange
        var factory = new AppDbContextFactory(mockDbCtxLogger.Object, _testConfiguration);

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
        var factory = new AppDbContextFactory(mockDbCtxLogger.Object, _testConfiguration);

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
        GC.SuppressFinalize(this);
    }
}
