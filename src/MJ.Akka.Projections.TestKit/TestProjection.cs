using System.Collections.Concurrent;
using Akka;
using Akka.Actor;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.TestKit;

public class TestProjection<TId, TContext, TStorageSetup>(
    IProjection<TId, TContext, TStorageSetup> projectionToTest,
    params object[] events)
    : IProjection<TId, TContext, ProjectionTestStorageSetup>
    where TId : notnull where TContext : IProjectionContext where TStorageSetup : IStorageSetup
{
    public string Name => projectionToTest.Name;
    public TimeSpan ProjectionTimeout => projectionToTest.ProjectionTimeout;
    
    public Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
    {
        return Source.FromEnumerator(() => events
            .Select((evnt, index) => new EventWithPosition(evnt, index))
            .GetEnumerator());
    }

    public Props CreateCoordinatorProps(ISupplyProjectionConfigurations configSupplier)
    {
        return ProjectionsCoordinator.Init(configSupplier);
    }

    public Props CreateProjectionProps(ISupplyProjectionConfigurations configSupplier)
    {
        return DocumentProjection.Init(configSupplier);
    }

    public long? GetInitialPosition()
    {
        return null;
    }

    public ISetupProjectionHandlers<TId, TContext> Configure(ISetupProjection<TId, TContext> config)
    {
        return projectionToTest.Configure(config);
    }

    public ILoadProjectionContext<TId, TContext> GetLoadProjectionContext(ProjectionTestStorageSetup storageSetup)
    {
        return new ContextLoader(storageSetup.Storage, Name);
    }

    public TContext GetDefaultContext(TId id)
    {
        return projectionToTest.GetDefaultContext(id);
    }

    private class ContextLoader(
        ConcurrentDictionary<ProjectionContextId, IProjectionContext> storage,
        string projectionName) 
        : ILoadProjectionContext<TId, TContext>
    {
        public Task<TContext> Load(
            TId id, 
            Func<TId, TContext> getDefaultContext, 
            CancellationToken cancellationToken = default)
        {
            return storage.TryGetValue(new ProjectionContextId(projectionName, id), out var context) 
                ? Task.FromResult((TContext)context) 
                : Task.FromResult(getDefaultContext(id));
        }
    }
}