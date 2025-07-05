namespace MJ.Akka.Projections.Storage;

public interface IProjectionStorage
{
    Task<StoreProjectionResponse> Store(
        StoreProjectionRequest request,
        CancellationToken cancellationToken = default);
}