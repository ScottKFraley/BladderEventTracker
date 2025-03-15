namespace trackerApi.EndPoints;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using trackerApi.DbContext;
using trackerApi.Models;
using trackerApi.Services;

public class TrackerEndpoints : ITrackerEndpoints
{
    private readonly ITrackingLogService _trackingService;
    private readonly ILogger<TrackerEndpoints> _logger;

    public TrackerEndpoints(ITrackingLogService trackingService, ILogger<TrackerEndpoints> logger)
    {
        _trackingService = trackingService;
        _logger = logger;
    }


    /// <summary>
    /// Task<List<TrackingLogItem>> GetNDaysOfLogRecordsAsync(int numDays, Guid userId) ?!
    /// </summary>
    /// <param name="numDays"></param>
    /// <param name="userId"></param>
    /// <returns>
    /// A Task&lt;IResult&gt; instance.
    /// </returns>
    public async Task<IResult> GetLastNDaysLogRecordsAsync(int numDays, Guid userId)
    {
        try
        {
            _logger.LogInformation("Processing GET request for last {NumDays} days of tracking log records for user {UserId}", numDays, userId);

            if (userId == Guid.Empty)
            {
                return TypedResults.BadRequest("User ID is required");
            }

            var records = await _trackingService.GetNDaysOfLogRecordsAsync(numDays, userId);

            if (records.Count == 0)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GET request for tracking log records");

            return TypedResults.Problem("Error retrieving tracking log records");
        }
    }

    public async Task<IResult> GetLogRecords(Guid? userId = null)
    {
        try
        {
            _logger.LogInformation("Processing GET request for tracking log records");
            var records = await _trackingService.GetLogRecordsAsync(userId);

            return TypedResults.Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GET request for tracking log records");

            return TypedResults.Problem("Error retrieving tracking log records");
        }
    }

    public async Task<IResult> CreateLogRecord(TrackingLogItem logItem)
    {
        try
        {
            _logger.LogInformation("Processing POST request to create tracking log record");
            var createdItem = await _trackingService.CreateLogRecordAsync(logItem);

            return TypedResults.Created($"/tracking/{createdItem.Id}", createdItem);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating tracking log record");

            return TypedResults.BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Referenced user not found while creating tracking log record");

            return TypedResults.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing POST request to create tracking log record");

            return TypedResults.Problem("Error creating tracking log record");
        }
    }

    public void MapTrackerEndpoints(IEndpointRouteBuilder group)
    {
        // Group your endpoints under a common route
        // See Program.cs near the bottom.

        // // endpoint for GETTING tracker rows
        // group.MapGet(
        // MOVE THIS into its own method, as the below two Mappings are configured.

        group.MapGet("/all", GetLogRecords)
             .WithName("GetLogRecords")
             .WithOpenApi()
             .WithDescription("Gets all tracking log items, optionally filtered by user ID");

        // endpoint for GETTING tracker rows
        group.MapGet("/{numDays}", GetLastNDaysLogRecordsAsync)
            .WithName("GetTrackedEntries")
            .WithOpenApi()                      // do I want/need this, if I have the below?
            .Produces<TrackingLogItem>(200)     // Document response types
            .Produces(404)
            .RequireAuthorization();

        group.MapPost("/", CreateLogRecord)
             .WithName("CreateLogRecord")
             .WithOpenApi()
             .WithDescription("Creates a new tracking log item");
    }
}
