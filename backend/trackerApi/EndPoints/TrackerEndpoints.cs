
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using trackerApi.DbContext;
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



//public class TrackerEndpoints : ITrackerEndpoints
//{
//    private readonly ITrackingLogService _trackingService;
//    private readonly ILogger<TrackerEndpoints> _logger;

//    public TrackerEndpoints(ITrackingLogService trackingService, ILogger<TrackerEndpoints> logger)
//    {
//        _trackingService = trackingService;
//        _logger = logger;
//    }


//    /// <summary>
//    /// Task<List<TrackingLogItem>> GetNDaysOfLogRecordsAsync(int numDays, Guid userId) ?!
//    /// </summary>
//    /// <param name="numDays"></param>
//    /// <param name="userId"></param>
//    /// <returns>
//    /// A Task&lt;IResult&gt; instance.
//    /// </returns>
//    public async Task<IResult> GetLastNDaysLogRecordsAsync(int numDays, Guid userId)
//    {
//        try
//        {
//            _logger.LogInformation("Processing GET request for last {NumDays} days of tracking log records for user {UserId}", numDays, userId);

//            if (userId == Guid.Empty)
//            {
//                return TypedResults.BadRequest("User ID is required");
//            }

//            var records = await _trackingService.GetNDaysOfLogRecordsAsync(numDays, userId);

//            // if no records are found, I want it to just return an empty list, not a 404

//            return TypedResults.Ok(records);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error processing GET request for tracking log records");

//            return TypedResults.Problem("Error retrieving tracking log records");
//        }
//    }

//    public async Task<IResult> GetLogRecords(Guid? userId = null)
//    {
//        try
//        {
//            _logger.LogInformation("Processing GET request for tracking log records");
//            var records = await _trackingService.GetLogRecordsAsync(userId);

//            return TypedResults.Ok(records);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error processing GET request for tracking log records");

//            return TypedResults.Problem("Error retrieving tracking log records");
//        }
//    }

//    public async Task<IResult> CreateLogRecord(TrackingLogItem logItem)
//    {
//        try
//        {
//            _logger.LogInformation("Processing POST request to create tracking log record");
//            var createdItem = await _trackingService.CreateLogRecordAsync(logItem);

//            return TypedResults.Created($"/tracking/{createdItem.Id}", createdItem);
//        }
//        catch (ArgumentException ex)
//        {
//            _logger.LogWarning(ex, "Validation error while creating tracking log record");

//            return TypedResults.BadRequest(ex.Message);
//        }
//        catch (KeyNotFoundException ex)
//        {
//            _logger.LogWarning(ex, "Referenced user not found while creating tracking log record");

//            return TypedResults.NotFound(ex.Message);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error processing POST request to create tracking log record");

//            return TypedResults.Problem("Error creating tracking log record");
//        }
//    }

//    public void MapTrackerEndpoints(IEndpointRouteBuilder group)
//    {
//        // Grouping these endpoints under a common route.
//        // See Program.cs near the bottom.

//        group.MapGet("/all", GetLogRecords)
//             .WithName("GetLogRecords")
//             .WithOpenApi()
//             .WithDescription("Gets all tracking log items, optionally filtered by user ID");

//        // endpoint for GETTING tracker rows
//        group.MapGet("/{numDays}/{userId}", GetLastNDaysLogRecordsAsync)
//            .WithName("GetTrackedEntries")
//            .WithOpenApi()                      // do I want/need this, if I have the below?
//            .Produces<TrackingLogItem>(200)     // Document response types
//            .Produces(404)
//            .RequireAuthorization();

//        group.MapPost("/", CreateLogRecord)
//             .WithName("CreateLogRecord")
//             .WithOpenApi()
//             .WithDescription("Creates a new tracking log item");
//    }
//}
