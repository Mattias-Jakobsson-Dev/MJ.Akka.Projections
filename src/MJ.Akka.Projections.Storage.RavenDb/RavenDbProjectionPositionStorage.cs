using Raven.Client.Documents;

namespace MJ.Akka.Projections.Storage.RavenDb;

public class RavenDbProjectionPositionStorage(IDocumentStore documentStore) : IProjectionPositionStorage
{
    public async Task<long?> LoadLatestPosition(
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();

        var projectionPosition = await session.LoadAsync<ProjectionPosition>(
            ProjectionPosition.BuildId(projectionName),
            cancellationToken);

        return projectionPosition?.Position;
    }

    public async Task<long?> StoreLatestPosition(
        string projectionName,
        long? position,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();
        
        var projectionPosition = await session.LoadAsync<ProjectionPosition>(
            ProjectionPosition.BuildId(projectionName), 
            cancellationToken);

        if (projectionPosition == null)
        {
            projectionPosition = new ProjectionPosition(projectionName);

            await session.StoreAsync(projectionPosition, cancellationToken);
        }

        if (position != null && position > projectionPosition.Position)
            projectionPosition.Position = position.Value;

        await session.SaveChangesAsync(cancellationToken);

        return projectionPosition.Position;
    }

    public async Task Reset(string projectionName, long? position = null, CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();
        
        var projectionPosition = await session.LoadAsync<ProjectionPosition>(
            ProjectionPosition.BuildId(projectionName), 
            cancellationToken);
        
        if (projectionPosition == null)
        {
            projectionPosition = new ProjectionPosition(projectionName);

            await session.StoreAsync(projectionPosition, cancellationToken);
        }
        
        projectionPosition.Position = position ?? 0;
        
        await session.SaveChangesAsync(cancellationToken);
    }
}