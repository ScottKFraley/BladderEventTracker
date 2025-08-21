using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using trackerApi.Models;

namespace trackerApi.EndPoints;

public static class DebugEndpoints
{
    public static void MapDebugEndpoints(this IEndpointRouteBuilder app)
    {
        var debugGroup = app.MapGroup("/api/debug")
            .WithTags("Debug")
            .WithOpenApi();

        // Debug info endpoint - requires authentication
        debugGroup.MapGet("/info", GetDebugInfo)
            .RequireAuthorization()
            .WithName("GetDebugInfo")
            .WithSummary("Get comprehensive debug information")
            .WithDescription("Returns detailed debug information about the user session, authentication, and system state")
            .Produces<DebugInfoResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        // Enhanced error simulation for testing
        debugGroup.MapPost("/simulate-error", SimulateError)
            .RequireAuthorization()
            .WithName("SimulateError")
            .WithSummary("Simulate various error conditions for testing")
            .WithDescription("Allows testing of error handling by simulating different error scenarios")
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        // Get logs for correlation ID
        debugGroup.MapGet("/logs/{correlationId}", GetLogsByCorrelationId)
            .RequireAuthorization()
            .WithName("GetLogsByCorrelationId")
            .WithSummary("Get logs by correlation ID")
            .WithDescription("Retrieves all log entries associated with a specific correlation ID")
            .Produces<LogsResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetDebugInfo(
        HttpContext context,
        ILogger<Program> logger)
    {
        try
        {
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                ?? Guid.NewGuid().ToString();

            logger.LogInformation("Debug info requested with correlation ID: {CorrelationId}", correlationId);

            var userClaims = context.User?.Claims?.ToDictionary(c => c.Type, c => c.Value) ?? new Dictionary<string, string>();
            var userId = context.User?.FindFirst("sub")?.Value ?? context.User?.FindFirst("userId")?.Value;
            var username = context.User?.Identity?.Name ?? context.User?.FindFirst("unique_name")?.Value;

            var debugInfo = new DebugInfoResponse
            {
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                User = new UserDebugInfo
                {
                    UserId = userId,
                    Username = username,
                    IsAuthenticated = context.User?.Identity?.IsAuthenticated ?? false,
                    Claims = userClaims,
                    AuthenticationType = context.User?.Identity?.AuthenticationType
                },
                Request = new RequestDebugInfo
                {
                    Method = context.Request.Method,
                    Path = context.Request.Path,
                    QueryString = context.Request.QueryString.ToString(),
                    Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                    RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers.UserAgent.ToString()
                },
                Server = new ServerDebugInfo
                {
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    MachineName = Environment.MachineName,
                    ProcessId = Environment.ProcessId,
                    WorkingSet = Environment.WorkingSet,
                    ServerTime = DateTime.UtcNow,
                    AppVersion = "1.0.0" // You might want to get this from assembly info
                }
            };

            logger.LogInformation("Debug info compiled successfully for user {UserId}", userId);
            return Results.Ok(debugInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to compile debug information");
            
            var errorResponse = new ErrorResponse
            {
                Error = "INTERNAL_SERVER_ERROR",
                Message = "Failed to retrieve debug information",
                Details = ex.Message,
                CorrelationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault(),
                Timestamp = DateTime.UtcNow
            };

            return Results.Problem(
                detail: errorResponse.Details,
                instance: context.Request.Path,
                statusCode: StatusCodes.Status500InternalServerError,
                title: errorResponse.Error,
                type: "https://tools.ietf.org/html/rfc7231#section-6.6.1");
        }
    }

    private static async Task<IResult> SimulateError(
        [FromBody] ErrorSimulationRequest request,
        HttpContext context,
        ILogger<Program> logger)
    {
        try
        {
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                ?? Guid.NewGuid().ToString();

            logger.LogInformation("Error simulation requested: {ErrorType} with correlation ID: {CorrelationId}", 
                request.ErrorType, correlationId);

            var errorResponse = new ErrorResponse
            {
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            };

            switch (request.ErrorType?.ToLower())
            {
                case "timeout":
                    await Task.Delay(request.DelayMs ?? 5000); // Simulate timeout
                    errorResponse.Error = "REQUEST_TIMEOUT";
                    errorResponse.Message = "Request timed out (simulated)";
                    errorResponse.StatusCode = 408;
                    return Results.Problem(
                        detail: errorResponse.Message,
                        statusCode: StatusCodes.Status408RequestTimeout,
                        title: errorResponse.Error);

                case "unauthorized":
                    errorResponse.Error = "UNAUTHORIZED";
                    errorResponse.Message = "Authentication failed (simulated)";
                    errorResponse.StatusCode = 401;
                    errorResponse.SuggestedAction = "Check authentication credentials and retry";
                    return Results.Problem(
                        detail: errorResponse.Message,
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: errorResponse.Error);

                case "database":
                    logger.LogError("Simulated database error for correlation ID: {CorrelationId}", correlationId);
                    errorResponse.Error = "DATABASE_ERROR";
                    errorResponse.Message = "Database connection failed (simulated)";
                    errorResponse.StatusCode = 500;
                    errorResponse.Details = "SqlException: Timeout expired. The timeout period elapsed prior to completion of the operation";
                    return Results.Problem(
                        detail: errorResponse.Details,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: errorResponse.Error);

                case "validation":
                    errorResponse.Error = "VALIDATION_ERROR";
                    errorResponse.Message = "Invalid input data (simulated)";
                    errorResponse.StatusCode = 400;
                    errorResponse.Details = "The field 'testField' is required";
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]> { { "testField", new[] { "This field is required" } } },
                        detail: errorResponse.Message,
                        title: errorResponse.Error);

                default:
                    errorResponse.Error = "UNKNOWN_ERROR_TYPE";
                    errorResponse.Message = $"Unknown error type: {request.ErrorType}";
                    errorResponse.StatusCode = 400;
                    return Results.BadRequest(errorResponse);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during error simulation");
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "SIMULATION_ERROR");
        }
    }

    private static async Task<IResult> GetLogsByCorrelationId(
        string correlationId,
        HttpContext context,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Log retrieval requested for correlation ID: {CorrelationId}", correlationId);

            // This is a simplified implementation
            // In a real scenario, you would query your logging system (Application Insights, etc.)
            var mockLogs = new List<LogEntryResponse>
            {
                new()
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    Level = "Info",
                    Message = $"Request started with correlation ID: {correlationId}",
                    Source = "Backend",
                    CorrelationId = correlationId,
                    Details = new { RequestPath = context.Request.Path.ToString() }
                },
                new()
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-4),
                    Level = "Debug",
                    Message = "Processing authentication",
                    Source = "Backend",
                    CorrelationId = correlationId,
                    Details = new { UserId = context.User?.FindFirst("sub")?.Value }
                },
                new()
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-3),
                    Level = "Info",
                    Message = "Request completed successfully",
                    Source = "Backend",
                    CorrelationId = correlationId,
                    Details = new { Duration = "1500ms", StatusCode = 200 }
                }
            };

            var response = new LogsResponse
            {
                CorrelationId = correlationId,
                LogEntries = mockLogs,
                TotalCount = mockLogs.Count,
                RetrievedAt = DateTime.UtcNow
            };

            logger.LogInformation("Retrieved {LogCount} log entries for correlation ID: {CorrelationId}", 
                mockLogs.Count, correlationId);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve logs for correlation ID: {CorrelationId}", correlationId);
            
            var errorResponse = new ErrorResponse
            {
                Error = "LOG_RETRIEVAL_ERROR",
                Message = "Failed to retrieve logs",
                Details = ex.Message,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            };

            return Results.Problem(
                detail: errorResponse.Details,
                statusCode: StatusCodes.Status500InternalServerError,
                title: errorResponse.Error);
        }
    }
}

// Request/Response models for debug endpoints
public class DebugInfoResponse
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public UserDebugInfo User { get; set; } = new();
    public RequestDebugInfo Request { get; set; } = new();
    public ServerDebugInfo Server { get; set; } = new();
}

public class UserDebugInfo
{
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public bool IsAuthenticated { get; set; }
    public Dictionary<string, string> Claims { get; set; } = new();
    public string? AuthenticationType { get; set; }
}

public class RequestDebugInfo
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string QueryString { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? RemoteIpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class ServerDebugInfo
{
    public string Environment { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public long WorkingSet { get; set; }
    public DateTime ServerTime { get; set; }
    public string AppVersion { get; set; } = string.Empty;
}

public class ErrorSimulationRequest
{
    public string ErrorType { get; set; } = string.Empty; // "timeout", "unauthorized", "database", "validation"
    public int? DelayMs { get; set; } = 1000;
}

public class LogsResponse
{
    public string CorrelationId { get; set; } = string.Empty;
    public List<LogEntryResponse> LogEntries { get; set; } = new();
    public int TotalCount { get; set; }
    public DateTime RetrievedAt { get; set; }
}

public class LogEntryResponse
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public object? Details { get; set; }
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int StatusCode { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? SuggestedAction { get; set; }
}