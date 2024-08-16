using System.Collections.Immutable;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

public static class ProjectionsCoordinator
{
    public static class Commands
    {
        public record Start;

        public record Stop;

        public record Kill;

        public record WaitForCompletion;
    }

    public static class Responses
    {
        public record WaitForCompletionResponse(Exception? Error = null);
    }
}

[PublicAPI]
public class ProjectionsCoordinator<TId, TDocument> : ReceiveActor where TId : notnull where TDocument : notnull
{
    private static class InternalCommands
    {
        public record Fail(Exception Cause);

        public record Complete;
    }
    
    private readonly ILoggingAdapter _logger;

    private UniqueKillSwitch? _killSwitch;

    private readonly ProjectionConfiguration<TId, TDocument> _configuration;

    private IActorRef? _sequencer;
    
    public ProjectionsCoordinator()
    {
        _logger = Context.GetLogger();
        
        _configuration = Context
                             .System
                             .GetExtension<ProjectionConfiguration<TId, TDocument>>() ??
                         throw new NoDocumentProjectionException<TDocument>();

        Become(Stopped);
    }

    public static Props Init()
    {
        return Props.Create(() => new ProjectionsCoordinator<TId, TDocument>());
    }

    private void Stopped()
    {
        ReceiveAsync<ProjectionsCoordinator.Commands.Start>(async _ =>
        {
            _logger.Info("Starting projection {0}", _configuration.Projection.Name);

            var latestPosition = await _configuration.PositionStorage.LoadLatestPosition(_configuration.Projection.Name);

            _killSwitch = MaybeCreateRestartSource(() =>
                {
                    _logger.Info("Starting projection source for {0} from {1}", _configuration.Projection.Name, latestPosition);
                    
                    if (_sequencer != null)
                        Context.Stop(_sequencer);

                    var sequencer = ProjectionSequencer<TId, TDocument>.Create(Context);

                    _sequencer = sequencer;
                    
                    return _configuration
                        .StartSource(latestPosition)
                        .Batch(
                            _configuration.ProjectionStreamConfiguration.EventBatchSize,
                            ImmutableList.Create,
                            (current, item) => current.Add(item))
                        .SelectMany(data =>
                        {
                            return data
                                .SelectMany(x => _configuration
                                    .ProjectionsHandler
                                    .Transform(x.Event)
                                    .Select(y => x with { Event = y }))
                                .Select(x => new
                                {
                                    Event = x,
                                    Id = _configuration.ProjectionsHandler.GetDocumentIdFrom(x.Event)
                                })
                                .GroupBy(x => x.Id)
                                .Select(x => (
                                    Events: x.Select(y => y.Event).ToImmutableList(),
                                    Id: x.Key))
                                .Where(x => !x.Events.IsEmpty)
                                .Select(x => new
                                {
                                    x.Events,
                                    x.Id,
                                    LowestEventNumber = x.Events.Select(y => y.Position).Min()
                                })
                                .OrderBy(x => x.LowestEventNumber)
                                .Select(x => new ProjectionSequencer<TId, TDocument>.Commands.StartProjecting(
                                    x.Id,
                                    x.Events));
                        })
                        .Ask<ProjectionSequencer<TId, TDocument>.Responses.StartProjectingResponse>(
                            sequencer,
                            TimeSpan.FromMinutes(1),
                            1)
                        .SelectAsync(
                            _configuration.ProjectionStreamConfiguration.ProjectionParallelism,
                            async data =>
                            {
                                var response = await data.Task;

                                return response switch
                                {
                                    Messages.Acknowledge ack => new PositionData(ack.Position),
                                    Messages.Reject nack => throw new Exception("Rejected projection", nack.Cause),
                                    _ => throw new Exception("Unknown projection response")
                                };
                            })
                        .GroupedWithin(
                            _configuration.ProjectionStreamConfiguration.PositionBatching.Number,
                            _configuration.ProjectionStreamConfiguration.PositionBatching.Timeout)
                        .SelectAsync(1, async positions =>
                        {
                            var highestPosition = positions.Select(x => x.Position).MaxBy(y => y);

                            latestPosition =
                                await _configuration.PositionStorage.StoreLatestPosition(_configuration.Projection.Name,
                                    highestPosition);

                            return NotUsed.Instance;
                        });
                }, _configuration.RestartSettings)
                .ViaMaterialized(KillSwitches.Single<NotUsed>(), Keep.Right)
                .ToMaterialized(Sink.ActorRef<NotUsed>(
                    Self,
                    new InternalCommands.Complete(),
                    ex => new InternalCommands.Fail(ex)), Keep.Left)
                .Run(Context.System.Materializer());

            Become(Started);
        });

        Receive<ProjectionsCoordinator.Commands.Kill>(_ =>
        {
            Context.Stop(Self);
        });
    }

    private void Started()
    {
        var waitingForCompletion = new HashSet<IActorRef>();

        Receive<ProjectionsCoordinator.Commands.Stop>(_ =>
        {
            _logger.Info("Stopping projection {0}", _configuration.Projection.Name);

            _killSwitch?.Shutdown();
            
            if (_sequencer != null)
                Context.Stop(_sequencer);

            foreach (var item in waitingForCompletion)
                item.Tell(new ProjectionsCoordinator.Responses.WaitForCompletionResponse());

            waitingForCompletion.Clear();

            Become(Stopped);
        });

        Receive<InternalCommands.Fail>(cmd =>
        {
            _logger.Error(cmd.Cause, "Projection {0} failed", _configuration.Projection.Name);

            _killSwitch?.Shutdown();
            
            if (_sequencer != null)
                Context.Stop(_sequencer);

            foreach (var item in waitingForCompletion)
                item.Tell(new ProjectionsCoordinator.Responses.WaitForCompletionResponse(cmd.Cause));

            waitingForCompletion.Clear();

            Become(Stopped);
        });

        Receive<ProjectionsCoordinator.Commands.WaitForCompletion>(_ => { waitingForCompletion.Add(Sender); });

        Receive<InternalCommands.Complete>(_ =>
        {
            if (_sequencer != null)
                Context.Stop(_sequencer);
            
            foreach (var item in waitingForCompletion)
                item.Tell(new ProjectionsCoordinator.Responses.WaitForCompletionResponse());

            waitingForCompletion.Clear();

            Become(Completed);
        });
        
        Receive<ProjectionsCoordinator.Commands.Kill>(_ =>
        {
            _logger.Info("Killing projection {0}", _configuration.Projection.Name);

            _killSwitch?.Shutdown();
            
            Context.Stop(Self);
        });
    }

    private void Completed()
    {
        Receive<ProjectionsCoordinator.Commands.WaitForCompletion>(_ =>
        {
            Sender.Tell(new ProjectionsCoordinator.Responses.WaitForCompletionResponse());
        });
        
        Receive<ProjectionsCoordinator.Commands.Kill>(_ =>
        {
            Context.Stop(Self);
        });
    }

    protected override void PreStart()
    {
        Self.Tell(new ProjectionsCoordinator.Commands.Start());

        base.PreStart();
    }
    
    private static Source<NotUsed, NotUsed> MaybeCreateRestartSource(
        Func<Source<NotUsed, NotUsed>> createSource,
        RestartSettings? restartSettings)
    {
        return restartSettings != null
            ? RestartSource.OnFailuresWithBackoff(createSource, restartSettings)
            : createSource();
    }

    private record PositionData(long? Position);
}