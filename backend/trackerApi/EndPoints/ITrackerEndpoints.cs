using trackerApi.Models;

namespace trackerApi.EndPoints;

public interface ITrackerEndpoints
{
    Task<IResult> GetLogRecords(Guid? userId = null);
    Task<IResult> CreateLogRecord(TrackingLogItem logItem);
    void MapEndpoints(IEndpointRouteBuilder group);
}
