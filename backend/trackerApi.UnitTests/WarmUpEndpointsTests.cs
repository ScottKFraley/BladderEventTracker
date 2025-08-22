using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using trackerApi.DbContext;
using trackerApi.EndPoints;

namespace trackerApi.UnitTests;

[ExcludeFromCodeCoverage]
public class WarmUpEndpointsTests : IDisposable
{
    private readonly AppDbContext _dbContext;

    public WarmUpEndpointsTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Create a mock logger for AppDbContext
        var mockLogger = new Mock<ILogger<AppDbContext>>();
        _dbContext = new AppDbContext(options, mockLogger.Object);
    }

    [Fact, Trait("Category", "Unit")]
    public async Task HandleWarmUp_ReturnsNoContent_WhenSuccessful()
    {
        // Act
        var result = await WarmUpEndpoints.HandleWarmUp(_dbContext);

        // Assert
        Assert.IsType<NoContent>(result);
    }

    [Fact, Trait("Category", "Unit")]
    public async Task HandleWarmUp_RespondsQuickly_UnderNormalConditions()
    {
        // Arrange
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var result = await WarmUpEndpoints.HandleWarmUp(_dbContext);
        stopwatch.Stop();

        // Assert
        Assert.IsType<NoContent>(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Warm-up endpoint took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
