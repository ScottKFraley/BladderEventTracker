using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("trackerApi.UnitTests")]

namespace trackerApi.EndPoints;

public static class WarmUpEndpoints
{
    public static void MapWarmUpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/warmup");
        
        group.MapGet("/", HandleWarmUp)
             .WithName("WarmUp")
             .WithOpenApi()
             .WithDescription("Health check endpoint to warm up the service")
             .WithRequestTimeout(TimeSpan.FromMinutes(3))
             .AllowAnonymous();
    }

    internal static IResult HandleWarmUp(ILogger logger)
    {
        try
        {
            logger.LogInformation("Warm-up request received");
            return TypedResults.NoContent(); // 204 No Content
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing warm-up request");
            return TypedResults.Problem("Error processing warm-up request");
        }
    }
}