using trackerApi.Models;
using trackerApi.Services;

namespace trackerApi.EndPoints;

///// <summary>
///// These are the actual endpoint handler interfaces.
///// </summary>
//public interface ITrackerEndpoints
//{
//    Task<IResult> HandleGetLastNDaysLogRecords(
//            int numDays,
//            Guid userId,
//            ITrackingLogService trackingService,
//            ILogger<TrackerEndpoints> logger);

//    Task<IResult> HandleGetLogRecords(
//        ITrackingLogService trackingService,
//        ILogger<TrackerEndpoints> logger,
//        Guid? userId = null);

//    Task<IResult> CreateLogRecord(TrackingLogItem logItem);

//    void MapTrackerEndpoints(IEndpointRouteBuilder group);
//}
