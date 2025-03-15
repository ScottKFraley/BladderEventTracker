using trackerApi.Models;

namespace trackerApi.EndPoints;

/// <summary>
/// These are the actual endpoint handler interfaces.
/// </summary>
public interface ITrackerEndpoints
{
    Task<IResult> GetLastNDaysLogRecordsAsync(int numDays, Guid userId);

    Task<IResult> GetLogRecords(Guid? userId = null);
    
    Task<IResult> CreateLogRecord(TrackingLogItem logItem);
    
    void MapTrackerEndpoints(IEndpointRouteBuilder group);
}
