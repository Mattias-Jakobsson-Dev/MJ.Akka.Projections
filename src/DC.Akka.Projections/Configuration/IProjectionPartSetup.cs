using Akka.Actor;
using Akka.Streams;
using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Configuration;

[PublicAPI]
public interface IProjectionPartSetup<out T> where T : IProjectionPartSetup<T>
{
    ActorSystem ActorSystem { get; }

    T WithCoordinatorFactory(
        Func<Task<IActorRef>> factory);
    
    T WithProjectionFactory(
        Func<object, Task<IActorRef>> factory);

    T WithRestartSettings(
        RestartSettings restartSettings);
    
    T WithProjectionStreamConfiguration(
        ProjectionStreamConfiguration projectionStreamConfiguration);
    
    T WithProjectionStorage(IProjectionStorage storage);

    T WithPositionStorage(IProjectionPositionStorage positionStorage);
}