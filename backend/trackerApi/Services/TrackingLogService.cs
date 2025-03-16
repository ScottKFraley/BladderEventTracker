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

    public TrackingLogService(AppDbContext dbContext, ILogger<TrackingLogService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
        _logger.LogInformation("TrackingLogService constructed with DbContext instance.");
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

            // Your query logic here
            var nDaysAgo = DateTime.UtcNow.AddDays(-numDays);

            var trackedEvents = await _dbContext.TrackingLogs
                .Where(t => t.UserId == userId && t.EventDate >= nDaysAgo)
                .OrderByDescending(t => t.EventDate)
                .ToListAsync();

            if (trackedEvents == null)
            {
                const string MessageTemplate = "No logged events found for the last {NumDays} days";
                var message = string.Format(MessageTemplate.Replace("{NumDays}", "{0}"), numDays);

                _logger.LogWarning(MessageTemplate, numDays);

                throw new Exception(message);
            }

            _logger.LogInformation(
                "{trackedEvents.Count} tracking log record(s) found for the last {NumDays} days", 
                trackedEvents.Count, numDays);

            return trackedEvents;
        }
        catch (ObjectDisposedException ode)
        {
            _logger.LogError(ode, "DbContext was disposed when attempting to access it!");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tracking log records for the last {NumDays} days", numDays);

            throw;
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

            logItem.EventDate = DateTime.UtcNow;

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
