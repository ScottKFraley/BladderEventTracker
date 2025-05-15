// Services/TrackingLogService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using trackerApi.DbContext;
using trackerApi.Models;
using trackerApi.Services;

public class TrackingLogService : ITrackingLogService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TrackingLogService> _logger;
    private string _timeZoneId;
    private readonly TimeZoneInfo _targetTimeZone;

    public TrackingLogService(
        AppDbContext dbContext,
        ILogger<TrackingLogService> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
        _logger.LogInformation("TrackingLogService constructed with DbContext instance.");

        _timeZoneId = configuration.GetValue<string>("AppSettings:TimeZoneId")
            ?? "America/Los_Angeles";

        // Initialize the TimeZoneInfo with error handling
        try
        {
            _targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogWarning("Specified timezone {TimeZoneId} not found, falling back to local time", _timeZoneId);
            _targetTimeZone = TimeZoneInfo.Local;
        }
    }

    public async Task<IEnumerable<TrackingLogItem>> GetLogRecordsAsync(Guid? userId = null)
    {
        try
        {
            _logger.LogInformation("Retrieving tracking log records. UserId filter: {UserId}", userId);

            var query = _dbContext.TrackingLogs.AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(x => x.UserId == userId.Value);
            }

            var records = await query
                .OrderByDescending(x => x.EventDate)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} tracking log records", records.Count);
            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tracking log records for UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<TrackingLogItem>> GetNDaysOfLogRecordsAsync(int numDays, Guid userId)
    {
        try
        {
            _logger.LogInformation("Retrieving tracking log record(s) for the last {NumDays} days", numDays);

            if (_dbContext == null)
            {
                _logger.LogError("DbContext is null");
                throw new InvalidOperationException("DbContext is null");
            }

            // Get current Pacific time and set to midnight
            _logger.LogInformation(
                "The `_targetTimeZone.Id` is \"{TimeZoneId}\" <--------", _targetTimeZone.Id);

            var pacificNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _targetTimeZone);
            // get yesterday at midnight pacific time
            var pacificStartDate = pacificNow.Date.AddDays(-1);


            // TODO: The offset is still not right as I'm seeing data on the 15th at 1630 back to the 13th! still need to make the adjustment(s)


            // Since data is stored in Pacific time, use Pacific time directly
            // but ensure the Kind is set appropriately for the database
            var queryDate = DateTime.SpecifyKind(pacificStartDate, DateTimeKind.Utc);

            var trackedEvents = await _dbContext.TrackingLogs
                .Where(t => t.UserId == userId && t.EventDate >= queryDate)
                .OrderByDescending(t => t.EventDate)
                .ToListAsync();

            _logger.LogInformation(
                "Querying records from {PacificStartDate} Pacific Time",
                pacificStartDate);

            if (trackedEvents.Count == 0)
            {
                const string MessageTemplate = "No logged events found for the last {NumDays} days";
                var message = string.Format(MessageTemplate.Replace("{NumDays}", "{0}"), numDays);

                _logger.LogWarning(MessageTemplate, numDays);

                throw new Exception(message);
            }

            _logger.LogInformation(
                "{Count} tracking log record(s) found for the last {NumDays} days",
                trackedEvents.Count, numDays);

            return trackedEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tracking log records for the last {NumDays} days", numDays);
            throw;
        }
    }

    public async Task<TrackingLogItem> CreateLogRecordAsync(TrackingLogItem logItem)
    {
        try
        {
            _logger.LogInformation("Creating new tracking log record for UserId: {UserId}", logItem.UserId);

            if (logItem.UserId == Guid.Empty)
            {
                _logger.LogWarning("Attempt to create tracking log record with empty UserId");
                throw new ArgumentException("UserId is required");
            }

            var userExists = await _dbContext.Users.AnyAsync(u => u.Id == logItem.UserId);
            if (!userExists)
            {
                _logger.LogWarning("Attempt to create tracking log record for non-existent UserId: {UserId}", logItem.UserId);
                throw new KeyNotFoundException($"User with ID {logItem.UserId} not found");
            }

            // Get current Pacific time
            var pacificNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _targetTimeZone);
            // Store as Pacific time but with UTC Kind to match existing data
            logItem.EventDate = DateTime.SpecifyKind(pacificNow, DateTimeKind.Utc);

            await _dbContext.TrackingLogs.AddAsync(logItem);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully created tracking log record with Id: {LogItemId}", logItem.Id);

            return logItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tracking log record for UserId: {UserId}", logItem.UserId);
            throw;
        }
    }
}
