using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using System.Diagnostics.CodeAnalysis;

using trackerApi.DbContext;
using trackerApi.Services;

namespace trackerApi.UnitTests;

// Unit Tests
[ExcludeFromCodeCoverage]
public class AppDbContextUnitTests : IDisposable
{
    private readonly Mock<ILogger<AppDbContext>> _mockDbCtxLogger;
    private bool _disposed = false;

    public AppDbContextUnitTests()
    {
        _mockDbCtxLogger = new Mock<ILogger<AppDbContext>>();
    }

    [Fact, Trait("Category", "Unit")]
    public void CreateDbContext_ShouldCreateValidContext()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new AppDbContext(options, _mockDbCtxLogger.Object);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<AppDbContext>(context);
    }

    [Fact, Trait("Category", "Unit")]
    public void CreateDbContext_ShouldHaveValidInMemoryProvider()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new AppDbContext(options, _mockDbCtxLogger.Object);

        // Assert
        Assert.Equal("Microsoft.EntityFrameworkCore.InMemory", context.Database.ProviderName);
        Assert.True(context.Database.IsInMemory());
    }
    [Fact, Trait("Category", "Unit")]
    public void CreateDbContext_ShouldHaveCorrectDbSets()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new AppDbContext(options, _mockDbCtxLogger.Object);

        // Assert
        Assert.NotNull(context.Users);
        Assert.NotNull(context.TrackingLogs);
        Assert.NotNull(context.RefreshTokens);
    }

    [Fact, Trait("Category", "Unit")]
    public void CreateDbContext_ShouldAllowDatabaseOperations()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act & Assert
        using var context = new AppDbContext(options, _mockDbCtxLogger.Object);
        
        // Should be able to ensure database is created
        Assert.True(context.Database.EnsureCreated());
        
        // Should be able to query empty sets without errors
        Assert.Empty(context.Users.ToList());
        Assert.Empty(context.TrackingLogs.ToList());
        Assert.Empty(context.RefreshTokens.ToList());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Clean up managed resources if needed
            }
            _disposed = true;
        }
    }
}
