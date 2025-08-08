using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using trackerApi.EndPoints;

namespace trackerApi.UnitTests;

[ExcludeFromCodeCoverage]
public class WarmUpEndpointsTests
{

    [Fact, Trait("Category", "Unit")]
    public void HandleWarmUp_ReturnsNoContent_WhenSuccessful()
    {
        // Act
        var result = WarmUpEndpoints.HandleWarmUp();

        // Assert
        Assert.IsType<NoContent>(result);
    }

    [Fact, Trait("Category", "Unit")]
    public void HandleWarmUp_RespondsQuickly_UnderNormalConditions()
    {
        // Arrange
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var result = WarmUpEndpoints.HandleWarmUp();
        stopwatch.Stop();

        // Assert
        Assert.IsType<NoContent>(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Warm-up endpoint took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
    }
}
