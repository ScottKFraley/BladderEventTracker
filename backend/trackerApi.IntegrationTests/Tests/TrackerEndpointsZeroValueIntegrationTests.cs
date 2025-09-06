namespace trackerApi.IntegrationTests.Tests;

using Microsoft.Extensions.Logging;
using trackerApi.EndPoints;
using trackerApi.Models;
using trackerApi.Services;

/// <summary>
/// Integration tests specifically for 0 value validation through complete data flow
/// from API endpoints through service layer to database storage and retrieval.
/// </summary>
[Collection("Database")]
public class TrackerEndpointsZeroValueIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly ILogger<TrackerEndpoints> _logger;
    private readonly TrackingLogService _trackingService;
    private readonly User _testUser;

    public TrackerEndpointsZeroValueIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        
        // Setup logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<TrackerEndpoints>();
        
        // Create service with real dependencies
        _trackingService = new TrackingLogService(
            _fixture.Context,
            loggerFactory.CreateLogger<TrackingLogService>(),
            null! // Configuration not needed for this test
        );

        // Get test user from fixture
        _testUser = _fixture.Context.Users.First();
    }

    [Fact, Trait("Category", "Integration")]
    public async Task EndToEndDataFlow_CreateAndRetrieve_WithAllZeroValues()
    {
        // Arrange - Create tracking log item with all fields set to 0
        var logItem = new TrackingLogItem
        {
            UserId = _testUser.Id,
            EventDate = DateTime.Now,
            Accident = false,
            ChangePadOrUnderware = false,
            LeakAmount = 0,    // Minimum valid value
            Urgency = 0,       // Minimum valid value 
            AwokeFromSleep = false,
            PainLevel = 0,     // Minimum valid value
            Notes = "Test entry with all zero values"
        };

        // Act 1 - Create the record via endpoint
        var createResult = await TrackerEndpoints.HandleCreateLogRecord(
            logItem,
            _trackingService,
            _logger);

        // Assert 1 - Creation succeeded
        var createdResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<TrackingLogItem>>(createResult);
        var createdItem = createdResult.Value!;
        
        // Verify all zero values were preserved
        Assert.Equal(0, createdItem.LeakAmount);
        Assert.Equal(0, createdItem.Urgency);
        Assert.Equal(0, createdItem.PainLevel);
        Assert.NotEqual(Guid.Empty, createdItem.Id);

        // Act 2 - Retrieve the record via endpoint
        var getResult = await TrackerEndpoints.HandleGetLogRecords(
            _trackingService,
            _logger,
            _testUser.Id);

        // Assert 2 - Retrieval succeeded with zero values intact
        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<IEnumerable<TrackingLogItem>>>(getResult);
        var retrievedItems = okResult.Value!;
        
        var retrievedItem = retrievedItems.FirstOrDefault(x => x.Id == createdItem.Id);
        Assert.NotNull(retrievedItem);
        
        // Verify zero values survived the round trip
        Assert.Equal(0, retrievedItem.LeakAmount);
        Assert.Equal(0, retrievedItem.Urgency);
        Assert.Equal(0, retrievedItem.PainLevel);
        Assert.Equal("Test entry with all zero values", retrievedItem.Notes);
    }

    [Theory, Trait("Category", "Integration")]
    [InlineData(0, 0, 0)]     // All minimums
    [InlineData(3, 4, 10)]    // All maximums
    [InlineData(0, 4, 10)]    // Min LeakAmount, max others
    [InlineData(3, 0, 0)]     // Max LeakAmount, min others
    [InlineData(1, 0, 5)]     // Mixed with zeros
    [InlineData(2, 2, 0)]     // Zero pain level
    public async Task EndToEndDataFlow_BoundaryValueValidation(int leakAmount, int urgency, int painLevel)
    {
        // Arrange
        var logItem = new TrackingLogItem
        {
            UserId = _testUser.Id,
            EventDate = DateTime.Now.AddMinutes(-10), // Slight offset to avoid conflicts
            LeakAmount = leakAmount,
            Urgency = urgency,
            PainLevel = painLevel,
            Notes = $"Boundary test: L{leakAmount} U{urgency} P{painLevel}"
        };

        // Act - Create and verify in one flow
        var createResult = await TrackerEndpoints.HandleCreateLogRecord(
            logItem,
            _trackingService,
            _logger);

        // Assert - Creation should always succeed for valid boundary values
        var createdResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<TrackingLogItem>>(createResult);
        var createdItem = createdResult.Value!;
        
        Assert.Equal(leakAmount, createdItem.LeakAmount);
        Assert.Equal(urgency, createdItem.Urgency);
        Assert.Equal(painLevel, createdItem.PainLevel);

        // Verify database constraints allow these values
        var dbItem = await _fixture.Context.TrackingLogs.FindAsync(createdItem.Id);
        Assert.NotNull(dbItem);
        Assert.Equal(leakAmount, dbItem.LeakAmount);
        Assert.Equal(urgency, dbItem.Urgency);
        Assert.Equal(painLevel, dbItem.PainLevel);
    }

    [Fact(Skip = "Database CHECK constraints not implemented yet"), Trait("Category", "Integration")]
    public async Task DatabaseConstraints_PreventInvalidNegativeValues()
    {
        // Arrange - Create item with invalid negative values
        // Note: This bypasses model validation to test database constraints directly
        var logItem = new TrackingLogItem
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            EventDate = DateTime.Now,
            LeakAmount = -1,   // Invalid
            Urgency = -1,      // Invalid
            PainLevel = -1,    // Invalid
        };

        // Act & Assert - Database should reject this
        _fixture.Context.TrackingLogs.Add(logItem);
        
        var exception = await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            async () => await _fixture.Context.SaveChangesAsync());
        
        // Verify the exception is related to check constraints
        Assert.Contains("CHECK", exception.InnerException?.Message ?? exception.Message);
    }

    [Fact(Skip = "Database CHECK constraints not implemented yet"), Trait("Category", "Integration")]
    public async Task DatabaseConstraints_PreventInvalidMaximumValues()
    {
        // Arrange - Create item with values exceeding maximum ranges
        var logItem = new TrackingLogItem
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            EventDate = DateTime.Now,
            LeakAmount = 4,    // Invalid (max is 3)
            Urgency = 5,       // Invalid (max is 4)
            PainLevel = 11,    // Invalid (max is 10)
        };

        // Act & Assert - Database should reject this
        _fixture.Context.TrackingLogs.Add(logItem);
        
        var exception = await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            async () => await _fixture.Context.SaveChangesAsync());
        
        // Verify the exception is related to check constraints
        Assert.Contains("CHECK", exception.InnerException?.Message ?? exception.Message);
    }

    [Fact, Trait("Category", "Integration")]
    public async Task GetNDaysOfRecords_ReturnsZeroValueEntries()
    {
        // Arrange - Create multiple entries with zero values across different days
        var now = DateTime.Now;
        var logItems = new[]
        {
            new TrackingLogItem
            {
                UserId = _testUser.Id,
                EventDate = now.AddDays(-1),
                LeakAmount = 0, Urgency = 0, PainLevel = 0,
                Notes = "Day -1 with zeros"
            },
            new TrackingLogItem
            {
                UserId = _testUser.Id,
                EventDate = now.AddDays(-2),
                LeakAmount = 0, Urgency = 2, PainLevel = 0,
                Notes = "Day -2 mixed with zeros"
            },
            new TrackingLogItem
            {
                UserId = _testUser.Id,
                EventDate = now.AddDays(-3),
                LeakAmount = 1, Urgency = 0, PainLevel = 0,
                Notes = "Day -3 partial zeros"
            }
        };

        // Create all entries
        foreach (var item in logItems)
        {
            await TrackerEndpoints.HandleCreateLogRecord(item, _trackingService, _logger);
        }

        // Act - Get last 5 days of records
        var result = await TrackerEndpoints.HandleGetLastNDaysLogRecords(
            5, _testUser.Id, _trackingService, _logger);

        // Assert - All entries should be returned with zero values intact
        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<TrackingLogItem>>>(result);
        var retrievedItems = okResult.Value!;
        
        // Should have at least our 3 test entries (may have more from other tests)
        Assert.True(retrievedItems.Count >= 3);
        
        // Verify zero values are preserved
        var zeroEntries = retrievedItems.Where(x => x.Notes != null && x.Notes.Contains("zeros")).ToList();
        Assert.True(zeroEntries.All(x => x.LeakAmount >= 0 && x.Urgency >= 0 && x.PainLevel >= 0));
        
        var allZeroEntry = zeroEntries.FirstOrDefault(x => x.Notes!.Contains("Day -1"));
        Assert.NotNull(allZeroEntry);
        Assert.Equal(0, allZeroEntry.LeakAmount);
        Assert.Equal(0, allZeroEntry.Urgency);
        Assert.Equal(0, allZeroEntry.PainLevel);
    }

    [Fact, Trait("Category", "Integration")]
    public async Task Debug_DirectDbContextTest()
    {
        // Test direct database interaction without endpoints
        var logItem = new TrackingLogItem
        {
            UserId = _testUser.Id,
            EventDate = DateTime.Now,
            LeakAmount = 0,    // Explicitly set to 0
            Urgency = 0,       // Explicitly set to 0
            PainLevel = 0,     // Explicitly set to 0
            Notes = "Debug test with zeros"
        };

        // Add directly to context
        _fixture.Context.TrackingLogs.Add(logItem);
        await _fixture.Context.SaveChangesAsync();

        // Retrieve directly from context
        var retrieved = await _fixture.Context.TrackingLogs.FindAsync(logItem.Id);
        
        // Check what was actually saved
        Assert.NotNull(retrieved);
        Assert.Equal(0, retrieved.LeakAmount);  // This should pass if DB is working correctly
        Assert.Equal(0, retrieved.Urgency);
        Assert.Equal(0, retrieved.PainLevel);
    }
}