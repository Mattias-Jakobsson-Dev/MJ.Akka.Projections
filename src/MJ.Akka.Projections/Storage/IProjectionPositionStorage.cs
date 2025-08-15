namespace MJ.Akka.Projections.Storage;

public interface IProjectionPositionStorage
{
    Task<long?> LoadLatestPosition(
        string projectionName,
        CancellationToken cancellationToken = default);
    
    Task<long?> StoreLatestPosition(
        string projectionName,
        long? position, 
        CancellationToken cancellationToken = default);
    
    Task Reset(
        string projectionName, 
        long? position = null,
        CancellationToken cancellationToken = default);
}