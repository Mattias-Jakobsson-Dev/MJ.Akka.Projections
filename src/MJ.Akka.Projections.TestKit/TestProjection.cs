using System.Collections.Concurrent;
using Akka;
using Akka.Actor;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.TestKit;

public class TestProjection<TIdContext, TContext, TStorageSetup>(
    IProjection<TIdContext, TContext, TStorageSetup> projectionToTest,
    params object[] events)
    : IProjection<TIdContext, TContext, ProjectionTestStorageSetup>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext where TStorageSetup : IStorageSetup
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

    public ISetupProjectionHandlers<TIdContext, TContext> Configure(ISetupProjection<TIdContext, TContext> config)
    {
        return projectionToTest.Configure(config);
    }

    public ILoadProjectionContext<TIdContext, TContext> GetLoadProjectionContext(ProjectionTestStorageSetup storageSetup)
    {
        return new ContextLoader(storageSetup.Storage, Name);
    }

    public TContext GetDefaultContext(TIdContext id)
    {
        return projectionToTest.GetDefaultContext(id);
    }

    private class ContextLoader(
        ConcurrentDictionary<ProjectionContextId, IProjectionContext> storage,
        string projectionName) 
        : ILoadProjectionContext<TIdContext, TContext>
    {
        public Task<TContext> Load(
            TIdContext id, 
            Func<TIdContext, TContext> getDefaultContext, 
            CancellationToken cancellationToken = default)
        {
            return storage.TryGetValue(new ProjectionContextId(projectionName, id), out var context) 
                ? Task.FromResult((TContext)context) 
                : Task.FromResult(getDefaultContext(id));
        }
    }
}