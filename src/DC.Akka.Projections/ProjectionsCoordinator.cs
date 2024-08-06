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

    public ProjectionsCoordinator(string projectionName)
    {
        _logger = Context.GetLogger();

        _configuration = Context
                             .System
                             .GetExtension<ProjectionsApplication>()
                             .GetProjectionConfiguration<TId, TDocument>(projectionName) ??
                         throw new NoDocumentProjectionException<TDocument>(projectionName);

        Become(Stopped);
    }

    private void Stopped()
    {
        ReceiveAsync<Commands.Start>(async _ =>
        {
            _logger.Info("Starting projection {Name}", _configuration.Name);

            var latestPosition = await _configuration.PositionStorage.LoadLatestPosition(_configuration.Name);

            _killSwitch = RestartSource
                .OnFailuresWithBackoff(() =>
                {
                    _logger.Info("Starting projection source for {Name} from {Position}", _configuration.Name, latestPosition);
                    
                    return _configuration
                        .StartSource(latestPosition)
                        .GroupedWithin(
                            _configuration.ProjectionStreamConfiguration.EventBatching.Number,
                            _configuration.ProjectionStreamConfiguration.EventBatching.Timeout)
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
                                    Id: x.Key));
                        })
                        .SelectAsync(
                            _configuration.ProjectionStreamConfiguration.ProjectionParallelism,
                            async data =>
                            {
                                if (data.Events.IsEmpty)
                                    return null;
                                
                                if (!data.Id.HasMatch || data.Id.Id == null)
                                    return data.Events.Select(x => x.Position).Max();

                                var projectionRef =
                                    await _configuration.CreateProjectionRef(data.Id.Id);

                                try
                                {
                                    var response = await Retries
                                        .Run<Messages.IProjectEventsResponse, AskTimeoutException>(
                                            () => projectionRef
                                                .Ask<Messages.IProjectEventsResponse>(
                                                    new DocumentProjection<TId, TDocument>.Commands.ProjectEvents(
                                                        data.Id,
                                                        data.Events),
                                                    _configuration.ProjectionStreamConfiguration.ProjectDocumentTimeout),
                                            _configuration.ProjectionStreamConfiguration.MaxProjectionRetries,
                                            (retries, exception) => _logger
                                                .Warning(
                                                    exception, 
                                                    "Failed handling {Count} events for {EntityId}, retrying (tries: {Tries})", 
                                                    data.Events.Count,
                                                    data.Id,
                                                    retries));

                                    return response switch
                                    {
                                        Messages.Acknowledge => data.Events.Select(x => x.Position).Max(),
                                        Messages.Reject nack => throw new Exception(
                                            "Rejected projection", nack.Cause),
                                        _ => throw new Exception("Unknown projection response")
                                    };
                                }
                                catch (Exception e)
                                {
                                    _logger
                                        .Error(e, "Failed handling {Count} events for {EntityId}", data.Events.Count, data.Id);

                                    throw;
                                }
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
            _logger.Info("Stopping projection {Name}", _configuration.Name);

            _killSwitch?.Shutdown();

            foreach (var item in waitingForCompletion)
                item.Tell(new Responses.WaitForCompletionResponse());

            waitingForCompletion.Clear();

            Become(Stopped);
        });

        Receive<Commands.Fail>(cmd =>
        {
            _logger.Error(cmd.Cause, "Projection {Name} failed", _configuration.Name);

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
        if (_configuration.AutoStart)
            Self.Tell(new Commands.Start());

        base.PreStart();
    }

    [PublicAPI]
    public class Proxy(IActorRef coordinator)
    {
        public void Start()
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
                timeout ?? TimeSpan.MaxValue);

            if (response.Error != null)
                throw response.Error;
        }

        public async Task RunToCompletion(TimeSpan? timeout = null)
        {
            Start();

            await WaitForCompletion(timeout);
        }
    }
}