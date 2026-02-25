using System.Collections.Immutable;
using Akka;
using Akka.Actor;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.InMemory;

namespace MJ.Akka.Projections.Tests.KeepTrackOfProjectorsInProcTests;

public class FakeProjection(TimeSpan delay) 
    : IProjection<SimpleIdContext<object>, InMemoryProjectionContext<SimpleIdContext<object>, object>, SetupInMemoryStorage>
{
    public string Name => GetType().Name;
    
    public TimeSpan ProjectionTimeout { get; } = TimeSpan.FromSeconds(5);

    public Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
    {
        return Source.From(ImmutableList<EventWithPosition>.Empty);
    }

    public Props CreateCoordinatorProps(ISupplyProjectionConfigurations configSupplier)
    {
        return ProjectionsCoordinator.Init(configSupplier);
    }

    public Props CreateProjectionProps(ISupplyProjectionConfigurations configSupplier)
    {
        return Props.Create(() => new FakeProjector(delay));
    }

    public long? GetInitialPosition()
    {
        return null;
    }

    public ISetupProjectionHandlers<SimpleIdContext<object>, InMemoryProjectionContext<SimpleIdContext<object>, object>> Configure(
        ISetupProjection<SimpleIdContext<object>, InMemoryProjectionContext<SimpleIdContext<object>, object>> config)
    {
        return config;
    }
    
    public ILoadProjectionContext<SimpleIdContext<object>, InMemoryProjectionContext<SimpleIdContext<object>, object>> GetLoadProjectionContext(
        SetupInMemoryStorage storageSetup)
    {
        return new InMemoryProjectionLoader<SimpleIdContext<object>, object>(
            id => storageSetup.LoadDocument(new ProjectionContextId(Name, id)));
    }

    public InMemoryProjectionContext<SimpleIdContext<object>, object> GetDefaultContext(SimpleIdContext<object> id)
    {
        return new InMemoryProjectionContext<SimpleIdContext<object>, object>(id, null);
    }

    private class FakeProjector : ReceiveActor
    {
        public FakeProjector(TimeSpan delay)
        {
            ReceiveAsync<DocumentProjection.Commands.ProjectEvents>(async cmd =>
            {
                await Task.Delay(delay);

                Sender.Tell(new Messages.Acknowledge(
                    cmd.Events.Select(x => x.Position).Max()));
            });
        }
    }
}