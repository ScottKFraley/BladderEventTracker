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
    private readonly Mock<ILogger> _mockLogger;

    public WarmUpEndpointsTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void HandleWarmUp_ReturnsNoContent_WhenSuccessful()
    {
        // Act
        var result = WarmUpEndpoints.HandleWarmUp(_mockLogger.Object);

        // Assert
        Assert.IsType<NoContent>(result);
    }

    [Fact]
    public void HandleWarmUp_LogsInformationMessage_WhenCalled()
    {
        // Act
        WarmUpEndpoints.HandleWarmUp(_mockLogger.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Warm-up request received")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void HandleWarmUp_RespondsQuickly_UnderNormalConditions()
    {
        // Arrange
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var result = WarmUpEndpoints.HandleWarmUp(_mockLogger.Object);
        stopwatch.Stop();

        // Assert
        Assert.IsType<NoContent>(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Warm-up endpoint took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
    }

    [Fact]
    public void HandleWarmUp_ReturnsProblem_WhenExceptionOccurs()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Throws(new InvalidOperationException("Test exception"));

        // Act
        var result = WarmUpEndpoints.HandleWarmUp(mockLogger.Object);

        // Assert
        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Contains("Error processing warm-up request", problemResult.ProblemDetails.Detail);
    }

    [Fact]
    public void HandleWarmUp_LogsError_WhenExceptionOccurs()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var testException = new InvalidOperationException("Test exception");
        
        // Only throw on the Information log, allow Error log to succeed
        mockLogger.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Throws(testException);

        // Act
        var result = WarmUpEndpoints.HandleWarmUp(mockLogger.Object);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing warm-up request")),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void HandleWarmUp_ReturnsConsistentResults_OnMultipleCalls(int numberOfCalls)
    {
        // Act & Assert
        for (int i = 0; i < numberOfCalls; i++)
        {
            var result = WarmUpEndpoints.HandleWarmUp(_mockLogger.Object);
            Assert.IsType<NoContent>(result);
        }

        // Verify logging was called the expected number of times
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Warm-up request received")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(numberOfCalls));
    }

    [Fact]
    public void HandleWarmUp_DoesNotRequireAuthentication_ByDesign()
    {
        // This test verifies that the handler method itself doesn't perform any authentication checks
        // The actual authentication bypass is configured at the endpoint mapping level
        
        // Act
        var result = WarmUpEndpoints.HandleWarmUp(_mockLogger.Object);

        // Assert
        Assert.IsType<NoContent>(result);
        // No authentication-related exceptions should be thrown
    }

    [Fact]
    public void HandleWarmUp_IsStateless_NoSideEffects()
    {
        // Arrange
        var logger1 = new Mock<ILogger>();
        var logger2 = new Mock<ILogger>();

        // Act
        var result1 = WarmUpEndpoints.HandleWarmUp(logger1.Object);
        var result2 = WarmUpEndpoints.HandleWarmUp(logger2.Object);

        // Assert
        Assert.IsType<NoContent>(result1);
        Assert.IsType<NoContent>(result2);
        
        // Each logger should have been called once independently
        logger1.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Warm-up request received")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        logger2.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Warm-up request received")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}