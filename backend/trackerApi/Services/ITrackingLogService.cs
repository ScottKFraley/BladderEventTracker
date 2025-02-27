using trackerApi.Models;

namespace trackerApi.Services;


public interface ITrackingLogService
{
    Task<IEnumerable<TrackingLogItem>> GetLogRecordsAsync(Guid? userId = null);

    Task<TrackingLogItem> CreateLogRecordAsync(TrackingLogItem logItem);
}
