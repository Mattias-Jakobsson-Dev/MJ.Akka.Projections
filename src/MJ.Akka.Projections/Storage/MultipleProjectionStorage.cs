namespace MJ.Akka.Projections.Storage;

public class MultipleProjectionStorage(params IProjectionStorage[] innerStorages) : IProjectionStorage
{
    public async Task<StoreProjectionResponse> Store(
        StoreProjectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = StoreProjectionResponse.Empty;
        
        foreach (var storage in innerStorages)
        {
            response = response.Merge(await storage.Store(request, cancellationToken));
        }

        return response;
    }
}