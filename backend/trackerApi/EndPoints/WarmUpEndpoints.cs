using Microsoft.EntityFrameworkCore;

using System.Runtime.CompilerServices;

using trackerApi.DbContext;

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

    internal static async Task<IResult> HandleWarmUp(AppDbContext dbContext)
    {
        try
        {
            // Check if we're using in-memory database (for testing)
            var isInMemoryDatabase = dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            
            if (isInMemoryDatabase)
            {
                // For in-memory database, just check if we can connect
                await dbContext.Database.CanConnectAsync();
            }
            else
            {
                // Set a timeout of 150 seconds / 2.5 minutes
                dbContext.Database.SetCommandTimeout(150);

                // For real databases, use raw SQL to wake up Azure SQL
                await dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
            }

            return TypedResults.NoContent(); // 204 No Content
        }
        catch (Exception ex)
        {
            return TypedResults.Problem($"Error processing warm-up request: {ex.Message}");
        }
    }
}
