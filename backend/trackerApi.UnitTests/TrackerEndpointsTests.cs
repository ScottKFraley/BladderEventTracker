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
    [Fact, Trait("Category", "Unit")]
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
    [Fact, Trait("Category", "Unit")]
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

    [Fact, Trait("Category", "Unit")]
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
    [Fact, Trait("Category", "Unit")]
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

    [Fact, Trait("Category", "Unit")]
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

    [Fact, Trait("Category", "Unit")]
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

    // Tests for 0 value validation
    [Fact, Trait("Category", "Unit")]
    public async Task HandleCreateLogRecord_ReturnsCreatedResult_WhenAllFieldsAreZero()
    {
        // Arrange - Create item with all range fields set to 0
        var userId = Guid.NewGuid();
        var logItem = new TrackingLogItem 
        { 
            UserId = userId,
            EventDate = DateTime.Now,
            LeakAmount = 0,  // Should be valid (Range 0-3)
            Urgency = 0,     // Should be valid (Range 0-4) 
            PainLevel = 0    // Should be valid (Range 0-10)
        };
        
        var createdItem = new TrackingLogItem 
        { 
            Id = Guid.NewGuid(),
            UserId = userId,
            EventDate = logItem.EventDate,
            LeakAmount = 0,
            Urgency = 0,
            PainLevel = 0
        };

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
        
        // Verify service was called with 0 values
        _mockTrackingService.Verify(s => s.CreateLogRecordAsync(It.Is<TrackingLogItem>(
            item => item.LeakAmount == 0 && item.Urgency == 0 && item.PainLevel == 0)), 
            Times.Once);
    }

    // Boundary value tests
    [Theory, Trait("Category", "Unit")]
    [InlineData(0, 0, 0)]     // All minimum values
    [InlineData(3, 4, 10)]    // All maximum values  
    [InlineData(1, 2, 5)]     // Mixed valid values
    [InlineData(0, 4, 10)]    // Min LeakAmount, max others
    [InlineData(3, 0, 0)]     // Max LeakAmount, min others
    public async Task HandleCreateLogRecord_ReturnsCreatedResult_WithBoundaryValues(
        int leakAmount, int urgency, int painLevel)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logItem = new TrackingLogItem
        {
            UserId = userId,
            EventDate = DateTime.Now,
            LeakAmount = leakAmount,
            Urgency = urgency,
            PainLevel = painLevel
        };

        var createdItem = new TrackingLogItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventDate = logItem.EventDate,
            LeakAmount = leakAmount,
            Urgency = urgency,
            PainLevel = painLevel
        };

        _mockTrackingService
            .Setup(s => s.CreateLogRecordAsync(It.IsAny<TrackingLogItem>()))
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
}
