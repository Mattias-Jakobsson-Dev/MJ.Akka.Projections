using Akka.Actor;

namespace DC.Akka.Projections;

public class ActorRefProjectionProxy(IActorRef coordinator, IProjection projection) : IProjectionProxy
{
    public IProjection Projection { get; } = projection;

    public Task Stop()
    {
        coordinator.Tell(new ProjectionsCoordinator.Commands.Stop());
        
        return Task.CompletedTask;
    }

    public async Task WaitForCompletion(TimeSpan? timeout = null)
    {
        var response = await coordinator.Ask<ProjectionsCoordinator.Responses.WaitForCompletionResponse>(
            new ProjectionsCoordinator.Commands.WaitForCompletion(),
            timeout ?? Timeout.InfiniteTimeSpan);

        if (response.Error != null)
            throw response.Error;
    }
}