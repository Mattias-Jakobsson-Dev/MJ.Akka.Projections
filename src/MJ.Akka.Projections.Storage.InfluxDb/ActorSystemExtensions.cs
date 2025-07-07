using Akka.Actor;
using InfluxDB.Client;
using JetBrains.Annotations;
using MJ.Akka.Projections.Configuration;

namespace MJ.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public static class ActorSystemExtensions
{
    public static IConfigureProjectionCoordinator InfluxDbProjections(
        this ActorSystem actorSystem,
        Func<
            IHaveConfiguration<ProjectionSystemConfiguration<SetupInfluxDbStorage>>,
            IHaveConfiguration<ProjectionSystemConfiguration<SetupInfluxDbStorage>>> configure,
        InfluxDBClient client,
        IProjectionPositionStorage positionStorage)
    {
        return actorSystem.Projections(
            configure,
            new SetupInfluxDbStorage(client, positionStorage));
    }
}