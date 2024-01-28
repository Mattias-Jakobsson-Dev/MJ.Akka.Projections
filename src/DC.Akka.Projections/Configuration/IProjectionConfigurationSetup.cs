using Akka.Actor;
using Akka.Streams;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public interface IProjectionConfigurationSetup<TDocument>
{
    ActorSystem System { get; }
    IProjection<TDocument> Projection { get; }
    
    IProjectionConfigurationSetup<TDocument> AutoStart();

    IProjectionConfigurationSetup<TDocument> WithCoordinatorFactory(
        Func<Task<IActorRef>> factory);
    
    IProjectionConfigurationSetup<TDocument> WithProjectionFactory(
        Func<object, Task<IActorRef>> factory);

    IProjectionConfigurationSetup<TDocument> WithRestartSettings(
        RestartSettings restartSettings);
    
    IProjectionConfigurationSetup<TDocument> WithProjectionStreamConfiguration(
        ProjectionStreamConfiguration projectionStreamConfiguration);
    
    IProjectionStorageConfigurationSetup<TDocument> WithStorage(IProjectionStorage storage);

    Task<ProjectionsCoordinator<TDocument>.Proxy> Build();
}