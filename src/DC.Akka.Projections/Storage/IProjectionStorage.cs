using System.Collections.Immutable;

namespace DC.Akka.Projections.Storage;

public interface IProjectionStorage
{
    Task<TDocument?> LoadDocument<TDocument>(object id, CancellationToken cancellationToken = default);

    Task StoreDocuments(
        IImmutableList<(ProjectedDocument Document, Action Ack, Action<Exception?> Nack)> documents,
        CancellationToken cancellationToken = default);
    
    Task<long?> LoadLatestPosition(
        string projectionName,
        CancellationToken cancellationToken = default);
    
    Task<long?> StoreLatestPosition(
        string projectionName,
        long? position, 
        CancellationToken cancellationToken = default);
}