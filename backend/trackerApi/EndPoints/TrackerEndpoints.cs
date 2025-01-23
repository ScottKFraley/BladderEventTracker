namespace trackerApi.EndPoints;

using Microsoft.EntityFrameworkCore;

using trackerApi.DbContext;
using trackerApi.Models;

public static class TrackerEndpoints
{
    public static void MapTrackerEndpoints(this WebApplication app)
    {
        // Group your endpoints under a common route
        var group = app.MapGroup("/api/v1/tracker");

        //group.MapGet("/", GetUsers);
        //group.MapGet("/{id}", GetUser);
        //group.MapPost("/", CreateUser);

        // endpoint for GETTING tracker rows
        group.MapGet(
                "/",
                async (AppDbContext context) =>
                {
                    var twoDaysAgo = DateTime.UtcNow.AddDays(-2);

                    var trackedEvents = await context.TrackingLogs
                        .Where(e => e.EventDate >= twoDaysAgo)
                        .OrderByDescending(e => e.EventDate)
                        .ToListAsync();

                    return trackedEvents;
                }
            )
            .WithName("GetTrackedEntry")
            .WithOpenApi()                      // do I want/need this, if I have the below?
            .Produces<TrackingLogItem>(200)     // Document response types
            .Produces(404)
            .RequireAuthorization();

        // ... other user-related endpoints
    }

}

