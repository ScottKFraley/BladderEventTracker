using Microsoft.AspNetCore.Http.HttpResults;
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
}

/*
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
}

*/