using System.Collections.Immutable;
using Akka;
using Akka.Actor;
using Akka.Streams.Dsl;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections.Tests.KeepTrackOfProjectorsInProcTests;

public class FakeProjection(TimeSpan delay) : IProjection<string, object>
{
    public string Name => GetType().Name;
    
    public TimeSpan ProjectionTimeout { get; } = TimeSpan.FromSeconds(5);

    public Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
    {
        return Source.From(ImmutableList<EventWithPosition>.Empty);
    }

    public Props CreateCoordinatorProps(ISupplyProjectionConfigurations configSupplier)
    {
        return ProjectionsCoordinator<string, object>.Init(configSupplier);
    }

    public Props CreateProjectionProps(object id, ISupplyProjectionConfigurations configSupplier)
    {
        return Props.Create(() => new FakeProjector(delay));
    }

    public string IdFromString(string id)
    {
        return id;
    }

    public string IdToString(string id)
    {
        return id;
    }

    public ISetupProjection<string, object> Configure(ISetupProjection<string, object> config)
    {
        return config;
    }

    private class FakeProjector : ReceiveActor
    {
        public FakeProjector(TimeSpan delay)
        {
            ReceiveAsync<DocumentProjection<string, object>.Commands.ProjectEvents>(async cmd =>
            {
                await Task.Delay(delay);

                Sender.Tell(new Messages.Acknowledge(
                    cmd.Events.Select(x => x.Position).Max()));
            });
        }
    }
}