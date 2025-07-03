using Akka.Actor;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.OneTime;

public static class ActorSystemExtensions
{
    public static IOneTimeProjection<TId, TDocument> CreateOneTimeProjection<TId, TDocument>(
        this ActorSystem actorSystem,
        IProjection<TId, TDocument> projection,
        Func<IHaveConfiguration<OneTimeProjectionConfig>, IHaveConfiguration<OneTimeProjectionConfig>>? configure = null)
        where TId : notnull where TDocument : notnull
    {
        var configuration = (configure ?? (c => c))(new ConfigureOneTimeProjection(
            actorSystem,
            OneTimeProjectionConfig.Default));

        var storage = new OneTimeProjectionStorage();

        var projectionCoordinator = actorSystem
            .Projections(config => config
                .WithProjectionStorage(storage)
                .WithPositionStorage(new StaticPositionStorage(configuration.Config.StartPosition))
                .WithRestartSettings(configuration.Config.RestartSettings)
                .WithEventBatchingStrategy(configuration.Config.EventBatchingStrategy!)
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                .WithProjection(projection));
        
        return new OneTimeProjection<TId, TDocument>(
            projectionCoordinator,
            projection.Name,
            storage);
    }

    private class OneTimeProjection<TId, TDocument>(
        IConfigureProjectionCoordinator coordinator,
        string projectionName,
        OneTimeProjectionStorage storage)
        : IOneTimeProjection<TId, TDocument>
        where TId : notnull
        where TDocument : notnull
    {
        public async Task<IOneTimeProjection<TId, TDocument>.IResult> Run(TimeSpan? timeout = null)
        {
            storage.Clear();
            
            await using var result = await coordinator.Start();

            var projectionProxy = result.Get(projectionName)!;
            
            await projectionProxy.WaitForCompletion(timeout);

            return new Result(storage);
        }
        
        private class Result(IProjectionStorage storage) : IOneTimeProjection<TId, TDocument>.IResult
        {
            public Task<TDocument?> Load(TId id)
            {
                return storage.LoadDocument<TDocument>(id);
            }
        }
    }
    
    private record ConfigureOneTimeProjection(
        ActorSystem ActorSystem,
        OneTimeProjectionConfig Config) : IHaveConfiguration<OneTimeProjectionConfig>
    {
        public IHaveConfiguration<OneTimeProjectionConfig> WithModifiedConfig(
            Func<OneTimeProjectionConfig, OneTimeProjectionConfig> modify)
        {
            return this with
            {
                Config = modify(Config)
            };
        }
    }
    
    private class StaticPositionStorage(long? startPosition) : IProjectionPositionStorage
    {
        public Task<long?> LoadLatestPosition(string projectionName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(startPosition);
        }

        public Task<long?> StoreLatestPosition(
            string projectionName,
            long? position,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(position);
        }
    }

    private class OneTimeProjectionStorage : InMemoryProjectionStorage
    {
        public void Clear()
        {
            Documents.Clear();
        }
    }
}