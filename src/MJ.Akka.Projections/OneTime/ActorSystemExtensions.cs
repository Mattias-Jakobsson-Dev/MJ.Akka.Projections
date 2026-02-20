using Akka.Actor;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.InMemory;

namespace MJ.Akka.Projections.OneTime;

public static class ActorSystemExtensions
{
    public static IOneTimeProjection<TId, TDocument> CreateOneTimeProjection<TId, TDocument>(
        this ActorSystem actorSystem,
        IProjection<TId, InMemoryProjectionContext<TId, TDocument>, SetupInMemoryStorage> projection,
        Func<IHaveConfiguration<OneTimeProjectionConfig>, IHaveConfiguration<OneTimeProjectionConfig>>? configure =
            null)
        where TId : notnull where TDocument : class
    {
        var configuration = (configure ?? (c => c))(new ConfigureOneTimeProjection(
            actorSystem,
            OneTimeProjectionConfig.Default));

        var storage = new SetupInMemoryStorage();

        var projectionCoordinator = actorSystem
            .Projections(config => config
                    .WithPositionStorage(new StaticPositionStorage(configuration.Config.StartPosition))
                    .WithRestartSettings(configuration.Config.RestartSettings)
                    .WithEventBatchingStrategy(configuration.Config.EventBatchingStrategy!)
                    .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                    .WithProjection(projection),
                storage);

        return new OneTimeProjection<TId, TDocument>(
            projectionCoordinator,
            projection,
            storage);
    }

    private class OneTimeProjection<TId, TDocument>(
        IConfigureProjectionCoordinator coordinator,
        IProjection<TId, InMemoryProjectionContext<TId, TDocument>, SetupInMemoryStorage> projection,
        SetupInMemoryStorage storageSetup)
        : IOneTimeProjection<TId, TDocument>
        where TId : notnull
        where TDocument : class
    {
        public async Task<IOneTimeProjection<TId, TDocument>.IResult> Run(TimeSpan? timeout = null)
        {
            storageSetup.Clear();

            await using var result = await coordinator.Start();

            var projectionProxy = result.Get(projection.Name)!;

            await projectionProxy.WaitForCompletion(timeout);

            return new Result(projection.GetLoadProjectionContext(storageSetup), projection);
        }

        private class Result(
            ILoadProjectionContext<TId, InMemoryProjectionContext<TId, TDocument>> loader,
            IProjection<TId, InMemoryProjectionContext<TId, TDocument>, SetupInMemoryStorage> projection)
            : IOneTimeProjection<TId, TDocument>.IResult
        {
            public async Task<TDocument?> Load(TId id)
            {
                var result = await loader.Load(id, projection.GetDefaultContext);

                return result.Document;
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

        public Task Reset(string projectionName, long? position = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}