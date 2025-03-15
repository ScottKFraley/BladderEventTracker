using trackerApi.Models;

namespace trackerApi.Services;


public interface ITrackingLogService
{
    Task<List<TrackingLogItem>> GetNDaysOfLogRecordsAsync(int numDays, Guid userId);

    Task<IEnumerable<TrackingLogItem>> GetLogRecordsAsync(Guid? userId = null);

    Task<TrackingLogItem> CreateLogRecordAsync(TrackingLogItem logItem);
}
