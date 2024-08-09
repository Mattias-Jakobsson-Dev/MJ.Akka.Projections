using System.Collections.Immutable;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

public class ProjectionsCoordinator<TId, TDocument> : ReceiveActor where TId : notnull where TDocument : notnull
{
    private static class Commands
    {
        public record Start;

        public record Stop;

        public record Fail(Exception Cause);

        public record Complete;

        public record WaitForCompletion;
    }

    private static class Responses
    {
        public record WaitForCompletionResponse(Exception? Error = null);
    }

    private readonly ILoggingAdapter _logger;

    private UniqueKillSwitch? _killSwitch;

    private readonly ProjectionConfiguration<TId, TDocument> _configuration;
    private readonly ProjectionSequencer<TId, TDocument>.Proxy _sequencer;

    public ProjectionsCoordinator(string projectionName)
    {
        _logger = Context.GetLogger();

        _sequencer = ProjectionSequencer<TId, TDocument>.Create(Context);

        _configuration = Context
                             .System
                             .GetExtension<ProjectionConfiguration<TId, TDocument>>() ??
                         throw new NoDocumentProjectionException<TDocument>(projectionName);

        Become(Stopped);
    }

    private void Stopped()
    {
        ReceiveAsync<Commands.Start>(async _ =>
        {
            _logger.Info("Starting projection {0}", _configuration.Name);

            var latestPosition = await _configuration.PositionStorage.LoadLatestPosition(_configuration.Name);
            await _sequencer.Clear();

            _killSwitch = MaybeCreateRestartSource(() =>
                {
                    _logger.Info("Starting projection source for {0} from {1}", _configuration.Name, latestPosition);
                    
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
                            _sequencer.Ref,
                            TimeSpan.FromMinutes(1),
                            1)
                        .SelectAsync(
                            _configuration.ProjectionStreamConfiguration.ProjectionParallelism,
                            async data =>
                            {
                                var response = await data.Task;

                                return response switch
                                {
                                    Messages.Acknowledge ack => ack.Position,
                                    Messages.Reject nack => throw new Exception("Rejected projection", nack.Cause),
                                    _ => throw new Exception("Unknown projection response")
                                };
                            })
                        .GroupedWithin(
                            _configuration.ProjectionStreamConfiguration.PositionBatching.Number,
                            _configuration.ProjectionStreamConfiguration.PositionBatching.Timeout)
                        .SelectAsync(1, async positions =>
                        {
                            var highestPosition = positions.MaxBy(y => y);

                            latestPosition =
                                await _configuration.PositionStorage.StoreLatestPosition(_configuration.Name,
                                    highestPosition);

                            return NotUsed.Instance;
                        });
                }, _configuration.RestartSettings)
                .ViaMaterialized(KillSwitches.Single<NotUsed>(), Keep.Right)
                .ToMaterialized(Sink.ActorRef<NotUsed>(
                    Self,
                    new Commands.Complete(),
                    ex => new Commands.Fail(ex)), Keep.Left)
                .Run(Context.System.Materializer());

            Become(Started);
        });
    }

    private void Started()
    {
        var waitingForCompletion = new HashSet<IActorRef>();

        Receive<Commands.Stop>(_ =>
        {
            _logger.Info("Stopping projection {0}", _configuration.Name);

            _killSwitch?.Shutdown();

            foreach (var item in waitingForCompletion)
                item.Tell(new Responses.WaitForCompletionResponse());

            waitingForCompletion.Clear();

            Become(Stopped);
        });

        Receive<Commands.Fail>(cmd =>
        {
            _logger.Error(cmd.Cause, "Projection {0} failed", _configuration.Name);

            _killSwitch?.Shutdown();

            foreach (var item in waitingForCompletion)
                item.Tell(new Responses.WaitForCompletionResponse(cmd.Cause));

            waitingForCompletion.Clear();

            Become(Stopped);
        });

        Receive<Commands.WaitForCompletion>(_ => { waitingForCompletion.Add(Sender); });

        Receive<Commands.Complete>(_ =>
        {
            foreach (var item in waitingForCompletion)
                item.Tell(new Responses.WaitForCompletionResponse());

            waitingForCompletion.Clear();

            Become(Completed);
        });
    }

    private void Completed()
    {
        Receive<Commands.WaitForCompletion>(_ => { Sender.Tell(new Responses.WaitForCompletionResponse()); });
    }

    protected override void PreStart()
    {
        Self.Tell(new Commands.Start());

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

    [PublicAPI]
    public class Proxy(IActorRef coordinator) : IProjectionProxy
    {
        internal void Start()
        {
            coordinator.Tell(new Commands.Start());
        }

        public void Stop()
        {
            coordinator.Tell(new Commands.Stop());
        }

        public async Task WaitForCompletion(TimeSpan? timeout = null)
        {
            var response = await coordinator.Ask<Responses.WaitForCompletionResponse>(
                new Commands.WaitForCompletion(),
                timeout ?? Timeout.InfiniteTimeSpan);

            if (response.Error != null)
                throw response.Error;
        }
    }
}