﻿// Services/TrackingLogService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using trackerApi.DbContext;
using trackerApi.Models;
using trackerApi.Services;

public class TrackingLogService : ITrackingLogService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TrackingLogService> _logger;

    public TrackingLogService(
        AppDbContext dbContext,
        ILogger<TrackingLogService> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        _logger = logger;
        _logger.LogInformation("TrackingLogService constructed with DbContext instance.");
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

            // Get current local [in my case, Pacific] time and calculate the cutoff date
            var localNow = DateTime.Now;
            var cutoffDate = localNow.Date.AddDays(-numDays);

            _logger.LogDebug("Querying for records since: {CutoffDate}", cutoffDate);

            // Query directly using Pacific time (no UTC conversion needed)
            var trackedEvents = await _dbContext.TrackingLogs
                .Where(t => t.UserId == userId && t.EventDate >= cutoffDate)
                .OrderByDescending(t => t.EventDate)
                .ToListAsync();

            _logger.LogInformation("{Count} tracking log record(s) found / returned.", trackedEvents.Count);

            return trackedEvents;
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
            _logger.LogInformation("Received EventDate: {EventDate}", logItem.EventDate);

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
