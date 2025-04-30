namespace trackerApi.UnitTests;

using Bogus;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

using Moq;

using System.Diagnostics.CodeAnalysis;

using trackerApi.EndPoints;
using trackerApi.Models;
using trackerApi.Services;
using trackerApi.UnitTests.Fakers;

[ExcludeFromCodeCoverage]
public class TrackerEndpointsTests
{
    private readonly Mock<ITrackingLogService> _mockTrackingService;
    private readonly Mock<ILogger<TrackerEndpoints>> _mockLogger;

    public TrackerEndpointsTests()
    {
        _mockTrackingService = new Mock<ITrackingLogService>();
        _mockLogger = new Mock<ILogger<TrackerEndpoints>>();
    }

    // Tests for HandleGetLogRecords
    [Fact]
    public async Task HandleGetLogRecords_ReturnsOkResult_WhenServiceSucceeds()
    {
        // Arrange
        // Generate a list of 5 fake items, with a specific user ID
        var userId = Guid.NewGuid();
        var fakeItemsWithUserId = TrackingLogItemFaker.Generate(5, userId);

        _mockTrackingService
            .Setup(s => s.GetLogRecordsAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(fakeItemsWithUserId);

        // Act
        var result = await TrackerEndpoints.HandleGetLogRecords(
            _mockTrackingService.Object,
            _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<Ok<IEnumerable<TrackingLogItem>>>(result);
        Assert.Equal(fakeItemsWithUserId, okResult.Value);
    }

    // Tests for HandleGetLastNDaysLogRecords
    [Fact]
    public async Task HandleGetLastNDaysLogRecords_ReturnsBadRequest_WhenUserIdEmpty()
    {
        // Arrange
        var numDays = 7;
        var emptyUserId = Guid.Empty;

        // Act
        var result = await TrackerEndpoints.HandleGetLastNDaysLogRecords(
            numDays,
            emptyUserId,
            _mockTrackingService.Object,
            _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequest<string>>(result);
        Assert.Equal("User ID is required", badRequestResult.Value);
    }

    [Fact]
    public async Task HandleGetLastNDaysLogRecords_ReturnsOkWithEmptyList_WhenNoRecordsFound()
    {
        // Arrange
        var numDays = 7;
        var userId = Guid.NewGuid();
        _mockTrackingService
            .Setup(s => s.GetNDaysOfLogRecordsAsync(numDays, userId))
            .ReturnsAsync(new List<TrackingLogItem>());

        // Act
        var result = await TrackerEndpoints.HandleGetLastNDaysLogRecords(
            numDays,
            userId,
            _mockTrackingService.Object,
            _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<Ok<List<TrackingLogItem>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Empty(okResult.Value);
    }

    // Tests for HandleCreateLogRecord
    [Fact]
    public async Task HandleCreateLogRecord_ReturnsCreatedResult_WhenSuccessful()
    {
        // Arrange
        var logItem = new TrackingLogItem { /* populate properties */ };
        var createdItem = new TrackingLogItem { Id = Guid.NewGuid() /* populate other properties */ };

        _mockTrackingService
            .Setup(s => s.CreateLogRecordAsync(logItem))
            .ReturnsAsync(createdItem);

        // Act
        var result = await TrackerEndpoints.HandleCreateLogRecord(
            logItem,
            _mockTrackingService.Object,
            _mockLogger.Object);

        // Assert
        var createdResult = Assert.IsType<Created<TrackingLogItem>>(result);
        Assert.Equal($"/tracking/{createdItem.Id}", createdResult.Location);
        Assert.Equal(createdItem, createdResult.Value);
    }

    [Fact]
    public async Task HandleCreateLogRecord_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var logItem = new TrackingLogItem();
        var expectedMessage = "Validation failed";

        _mockTrackingService
            .Setup(s => s.CreateLogRecordAsync(logItem))
            .ThrowsAsync(new ArgumentException(expectedMessage));

        // Act
        var result = await TrackerEndpoints.HandleCreateLogRecord(
            logItem,
            _mockTrackingService.Object,
            _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequest<string>>(result);
        Assert.Equal(expectedMessage, badRequestResult.Value);
    }

    [Fact]
    public async Task HandleCreateLogRecord_ReturnsNotFound_WhenUserNotFound()
    {
        // Arrange
        var logItem = new TrackingLogItem();
        var expectedMessage = "User not found";

        _mockTrackingService
            .Setup(s => s.CreateLogRecordAsync(logItem))
            .ThrowsAsync(new KeyNotFoundException(expectedMessage));

        // Act
        var result = await TrackerEndpoints.HandleCreateLogRecord(
            logItem,
            _mockTrackingService.Object,
            _mockLogger.Object);

        // Assert
        var notFoundResult = Assert.IsType<NotFound<string>>(result);
        Assert.Equal(expectedMessage, notFoundResult.Value);
    }
}
