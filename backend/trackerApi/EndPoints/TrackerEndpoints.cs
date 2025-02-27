namespace trackerApi.EndPoints;

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

    public void MapEndpoints(IEndpointRouteBuilder group)
    {
        // TODO: Q wants me to put the following in Program.cs, but I'd rather have it in here! Fix it!

        //var trackerEndpoints = app.Services.GetRequiredService<ITrackerEndpoints>();
        //var group = app.MapGroup("/tracking");
        //trackerEndpoints.MapEndpoints(group);

        // // Group your endpoints under a common route
        // var group = app.MapGroup("/api/v1/tracker");
        // USE THIS GROUPER ^^^^^^^

        // // endpoint for GETTING tracker rows
        // group.MapGet(
        // MOVE THIS into its own method, as the below two Mappings are configured.

        group.MapGet("/", GetLogRecords)
             .WithName("GetLogRecords")
             .WithOpenApi()
             .WithDescription("Gets all tracking log items, optionally filtered by user ID");

        group.MapPost("/", CreateLogRecord)
             .WithName("CreateLogRecord")
             .WithOpenApi()
             .WithDescription("Creates a new tracking log item");
    }
}

//public static class TrackerEndpoints
//{
//    public static void MapTrackerEndpoints(this WebApplication app)
//    {
//        // Group your endpoints under a common route
//        var group = app.MapGroup("/api/v1/tracker");

//        //group.MapGet("/{id}", GetLogRecordById);
//        group.MapPost("/", CreateLogRecord);

//        // endpoint for GETTING tracker rows
//        group.MapGet(
//                "/",
//                async (AppDbContext context) =>
//                {
//                    var twoDaysAgo = DateTime.UtcNow.AddDays(-2);

//                    var trackedEvents = await context.TrackingLogs
//                        .Where(e => e.EventDate >= twoDaysAgo)
//                        .OrderByDescending(e => e.EventDate)
//                        .ToListAsync();

//                    return trackedEvents;
//                }
//            )
//            .WithName("GetTrackedEntry")
//            .WithOpenApi()                      // do I want/need this, if I have the below?
//            .Produces<TrackingLogItem>(200)     // Document response types
//            .Produces(404)
//            .RequireAuthorization();

//        // ... other user-related endpoints
//    }
//}

