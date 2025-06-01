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
            _logger.LogInformation("Specified time zone after loading from appsettings; {TimeZoneId}", _timeZoneId);
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
        DateTime pacificStartDate = default;

        try
        {
            _logger.LogInformation("Retrieving tracking log record(s) for the last {NumDays} days", numDays);

            if (_dbContext == null)
            {
                _logger.LogError("DbContext is null");
                throw new InvalidOperationException("DbContext is null");
            }

            _logger.LogInformation(
                "The `_targetTimeZone.Id` is \"{TimeZoneId}\" <--------", _targetTimeZone.Id);

            // Get the current Pacific time
            var pacificNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _targetTimeZone);
            _logger.LogDebug("pacificNow is: {PacificDate}", pacificNow);

            // Get midnight of the current day in Pacific time
            var pacificMidnight = pacificNow.Date;

            // Go back 2 days to get the day before yesterday at midnight Pacific time
            var twoDaysAgoMidnight = pacificMidnight.AddDays(-numDays);
            _logger.LogDebug("Two days ago midnight is: {PacificDate}", twoDaysAgoMidnight);

            // Convert to UTC for the query (this will be ~7 or 8 hours ahead, so probably May 15th morning UTC)
            var utcQueryDate = TimeZoneInfo.ConvertTimeToUtc(twoDaysAgoMidnight, _targetTimeZone);

            // Then query for all records >= utcQueryDate
            var trackedEvents = await _dbContext.TrackingLogs
                .Where(t => t.UserId == userId && t.EventDate >= utcQueryDate)
                .OrderByDescending(t => t.EventDate)
                .ToListAsync();

            _logger.LogInformation(
                "Querying records from {PacificStartDate} Pacific Time",
                pacificStartDate);

            // Date/Times are stored as Pacific times, but set as UTC due to Postgres issue. -SKF
            //if (trackedEvents.Count > 0)
            //{
            //    var earliestDate = trackedEvents.Min(t => t.EventDate);
            //    var latestDate = trackedEvents.Max(t => t.EventDate);
            //    _logger.LogInformation(
            //        "Got events from {EarliestDate} to {LatestDate} UTC",
            //        earliestDate, latestDate);

            //    // Convert these to Pacific for easier debugging
            //    var earliestPacific = TimeZoneInfo.ConvertTimeFromUtc(earliestDate, _targetTimeZone);
            //    var latestPacific = TimeZoneInfo.ConvertTimeFromUtc(latestDate, _targetTimeZone);
            //    _logger.LogInformation(
            //        "Date range in Pacific: {EarliestPacific} to {LatestPacific}",
            //        earliestPacific, latestPacific);
            //}

            ////  make sure to remove this later
            //foreach (var evt in trackedEvents.Take(3)) // Just log first 3 events
            //{
            //    _logger.LogInformation("Raw stored EventDate: {EventDate} (Kind: {Kind})",
            //        evt.EventDate, evt.EventDate.Kind);
            //}

            _logger.LogInformation(
                "{Count} tracking log record(s) found / returned.",
                trackedEvents.Count);

            return trackedEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No TrackingLog records found since {YesterdayAtMidnight}", pacificStartDate);
            throw;
        }
    }

    public async Task<TrackingLogItem> CreateLogRecordAsync(TrackingLogItem logItem)
    {
        try
        {
            _logger.LogInformation("Creating new tracking log record for UserId: {UserId}", logItem.UserId);
            _logger.LogInformation("Received EventDate: {EventDate} (Offset: {Offset})",
                        logItem.EventDate, logItem.EventDate.Offset);

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

            // Create proper DateTimeOffset with Pacific timezone
            var pacificOffset = _targetTimeZone.GetUtcOffset(logItem.EventDate.DateTime);
            logItem.EventDate = new DateTimeOffset(logItem.EventDate.DateTime, pacificOffset);
            _logger.LogInformation("Created DateTimeOffset for storage: {EventDate}", logItem.EventDate);

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
