using Akka.Actor;
using Akka.Streams;
using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Configuration;

[PublicAPI]
public interface IProjectionPartSetup<out T> where T : IProjectionPartSetup<T>
{
    ActorSystem ActorSystem { get; }

    T WithCoordinatorFactory(IStartProjectionCoordinator factory);
    
    T WithProjectionFactory(IKeepTrackOfProjectors factory);

    T WithRestartSettings(RestartSettings restartSettings);
    
    T WithProjectionStreamConfiguration(ProjectionStreamConfiguration projectionStreamConfiguration);
    
    IProjectionStoragePartSetup<T> WithProjectionStorage(IProjectionStorage storage);

    T WithPositionStorage(IProjectionPositionStorage positionStorage);
}