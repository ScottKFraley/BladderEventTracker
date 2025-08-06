using trackerApi.Models;
using trackerApi.Services;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("trackerApi.UnitTests")]

namespace trackerApi.EndPoints;

/// <summary>
/// Map the Tracking Log entries endpoints.
/// </summary>
/// <remarks>
/// Don't need an interface because these are just mapping the endpoints and
/// shouldn't be getting called in any other way.
/// </remarks>
public class TrackerEndpoints //: ITrackerEndpoints
{
    // No need for constructor injection anymore
    public void MapTrackerEndpoints(IEndpointRouteBuilder group)
    {
        // TODO: So as not to confuse myself, let alone others, should rename "GetLogRecords" to "GetAllTrackerLogRecords"
        group.MapGet("/all", HandleGetLogRecords)
             .WithName("GetLogRecords")
             .WithOpenApi()
             .WithDescription("Gets all tracking log items, optionally filtered by user ID");

        group.MapGet("/{numDays}/{userId}", HandleGetLastNDaysLogRecords)
            .WithName("GetTrackedEntries")
            .WithOpenApi()
            .Produces<TrackingLogItem>(200)
            .Produces(404)
            .RequireAuthorization();

        group.MapPost("/", HandleCreateLogRecord)
             .WithName("CreateLogRecord")
             .WithOpenApi()
             .WithDescription("Creates a new tracking log item");
    }

    internal static async Task<IResult> HandleGetLogRecords(
        ITrackingLogService trackingService,
        ILogger<TrackerEndpoints> logger,
        Guid? userId = null)
    {
        try
        {
            logger.LogInformation("Processing GET request for tracking log records");
            var records = await trackingService.GetLogRecordsAsync(userId);

            return TypedResults.Ok(records);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing GET request for tracking log records");

            return TypedResults.Problem("Error retrieving tracking log records");
        }
    }

    internal static async Task<IResult> HandleGetLastNDaysLogRecords(
        int numDays,
        Guid userId,
        ITrackingLogService trackingService,
        ILogger<TrackerEndpoints> logger)
    {
        try
        {
            logger.LogInformation("Processing GET request for last {NumDays} days of tracking log records for user {UserId}",
                numDays, userId);

            if (userId == Guid.Empty)
            {
                return TypedResults.BadRequest("User ID is required");
            }

            var records = await trackingService.GetNDaysOfLogRecordsAsync(numDays, userId);

            // I want it to just return an empty collection if no records found,
            // not a 404. -SKF
            //if (records.Count == 0)
            //{
            //    return TypedResults.NotFound();
            //}

            return TypedResults.Ok(records);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing GET request for tracking log records");

            return TypedResults.Problem("Error retrieving tracking log records");
        }
    }

    internal static async Task<IResult> HandleCreateLogRecord(
        TrackingLogItem logItem,
        ITrackingLogService trackingService,
        ILogger<TrackerEndpoints> logger)
    {
        try
        {
            logger.LogInformation("Processing POST request to create tracking log record");
            var createdItem = await trackingService.CreateLogRecordAsync(logItem);
            return TypedResults.Created($"/tracking/{createdItem.Id}", createdItem);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error while creating tracking log record");
            return TypedResults.BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Referenced user not found while creating tracking log record");
            return TypedResults.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing POST request to create tracking log record");
            return TypedResults.Problem("Error creating tracking log record");
        }
    }
}
