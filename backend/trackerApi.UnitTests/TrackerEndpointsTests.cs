using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

using Moq;

using trackerApi.EndPoints;
using trackerApi.Models;
using trackerApi.Services;

public class TrackerEndpointsTests
{
    private readonly Mock<ITrackingLogService> _mockService;
    private readonly Mock<ILogger<TrackerEndpoints>> _mockLogger;
    private readonly ITrackerEndpoints _endpoints;

    public TrackerEndpointsTests()
    {
        _mockService = new Mock<ITrackingLogService>();
        _mockLogger = new Mock<ILogger<TrackerEndpoints>>();
        _endpoints = new TrackerEndpoints(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetLogRecords_ReturnsOkResult_WhenServiceSucceeds()
    {
        // Arrange
        var testRecords = new List<TrackingLogItem>
        {
            new TrackingLogItem { Id = Guid.NewGuid() }
        };
        _mockService.Setup(s => s.GetLogRecordsAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(testRecords);

        // Act
        var result = await _endpoints.GetLogRecords();

        // Assert
        var okResult = Assert.IsType<Ok<IEnumerable<TrackingLogItem>>>(result);
        Assert.Equal(testRecords, okResult.Value);
    }

    // Here's how to add logging to test(s)
    //[Fact]
    //public void ShouldLogError_WhenSomethingFails()
    //{
    //    // Arrange
    //    var loggerMock = new Mock<ILogger<TrackerEndpoints>>();
    //    var trackingService = new Mock<ITrackingLogService>();

    //    var endpoints = new TrackerEndpoints(
    //        trackingService.Object,
    //        loggerMock.Object
    //    );

    //    // Act
    //    // ... perform your test action ...

    //    // Assert
    //    loggerMock.Verify(
    //        x => x.Log(
    //            LogLevel.Error,
    //            It.IsAny<EventId>(),
    //            It.Is<It.IsAnyType>((v, t) => true),
    //            It.IsAny<Exception>(),
    //            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
    //        Times.Once);
    //}

    /* 
    // If you want to add a parameterized test, here's an example:
    [Theory]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void ShouldLog_WhenSpecifiedLevelOccurs(LogLevel level)
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TrackerEndpoints>>();
        var trackingService = new Mock<ITrackingLogService>();

        var endpoints = new TrackerEndpoints(
            trackingService.Object,
            loggerMock.Object
        );

        // Act
        // ... perform your test action ...

        // Assert
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
    */
}
