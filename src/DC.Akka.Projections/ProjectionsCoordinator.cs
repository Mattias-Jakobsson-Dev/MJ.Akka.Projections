using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Util;
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

        public record StopResponse;
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

    private readonly ProjectionConfiguration _configuration;

    private readonly HashSet<IActorRef> _waitingForCompletion = [];

    private ProjectionSequencer<TId, TDocument>.Proxy _sequencer;

    public ProjectionsCoordinator(string projectionName)
    {
        _logger = Context.GetLogger();

        _configuration = Context
                             .System
                             .GetExtension<ProjectionConfigurationsSupplier>()?
                             .GetConfigurationFor(projectionName) ??
                         throw new NoDocumentProjectionException<TDocument>(projectionName);

        _sequencer = ProjectionSequencer<TId, TDocument>.Create(Context, _configuration);
        
        Become(Stopped);
    }

    public static Props Init(string projectionName)
    {
        return Props.Create(() => new ProjectionsCoordinator<TId, TDocument>(projectionName));
    }

    private void Stopped()
    {
        ReceiveAsync<ProjectionsCoordinator.Commands.Start>(async _ =>
        {
            _logger.Info("Starting projection {0}", _configuration.Name);

            var latestPosition = await _configuration.PositionStorage.LoadLatestPosition(_configuration.Name);

            _killSwitch = MaybeCreateRestartSource(() =>
                {
                    _logger.Info("Starting projection source for {0} from {1}", _configuration.Name, latestPosition);
                    
                    var cancellation = new CancellationTokenSource();
                    
                    _sequencer.Reset(cancellation.Token);
                    
                    var source = _configuration.StartSource(latestPosition);

                    var flow = _configuration
                        .ProjectionEventBatchingStrategy
                        .Get(source)
                        .Select(x =>
                            new ProjectionSequencer<TId, TDocument>.Commands.StartProjecting(x))
                        .Ask<ProjectionSequencer<TId, TDocument>.Responses.StartProjectingResponse>(
                            _sequencer.Ref,
                            _configuration.GetProjection().ProjectionTimeout,
                            1)
                        .SelectMany(x => x.Tasks)
                        .SelectAsync(
                            _configuration.ProjectionEventBatchingStrategy.GetParallelism(),
                            async task =>
                            {
                                try
                                {
                                    var response = await task.task;

                                    return (task.groupId, response);
                                }
                                catch (Exception e)
                                {
                                    return (task.groupId, new Messages.Reject(e));
                                }
                            })
                        .Select(x =>
                        {
                            var position = x.response switch
                            {
                                Messages.Acknowledge ack => new PositionData(ack.Position),
                                Messages.Reject nack => throw new Exception("Rejected projection", nack.Cause),
                                null => throw new Exception("No projection response"),
                                _ => throw new Exception($"Unknown projection response: {x.response.GetType()}")
                            };
                            
                            return new ProjectionSequencer<TId, TDocument>.Commands.WaitForGroupToFinish(
                                x.groupId,
                                position);
                        })
                        .Ask<ProjectionSequencer<TId, TDocument>.Responses.WaitForGroupToFinishResponse>(
                            _sequencer.Ref,
                            _configuration.GetProjection().ProjectionTimeout,
                            _configuration.ProjectionEventBatchingStrategy.GetParallelism())
                        .Select(x => x.PositionData);

                    return _configuration
                        .PositionBatchingStrategy
                        .Get(flow)
                        .SelectAsync(1, async highestPosition =>
                        {
                            latestPosition = await _configuration.PositionStorage.StoreLatestPosition(
                                _configuration.Name,
                                highestPosition.Position, 
                                cancellation.Token);

                            return NotUsed.Instance;
                        })
                        .Recover(_ =>
                        {
                            cancellation.Cancel();

                            return Option<NotUsed>.None;
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

        Receive<ProjectionsCoordinator.Commands.Kill>(_ => { Context.Stop(Self); });

        Receive<ProjectionsCoordinator.Commands.Stop>(_ =>
        {
            Sender.Tell(new ProjectionsCoordinator.Responses.StopResponse());
        });

        Receive<ProjectionsCoordinator.Commands.WaitForCompletion>(_ => { _waitingForCompletion.Add(Sender); });
    }

    private void Started()
    {
        Receive<ProjectionsCoordinator.Commands.Stop>(_ =>
        {
            _logger.Info("Stopping projection {0}", _configuration.Name);

            _killSwitch?.Shutdown();

            _sequencer.Reset(CancellationToken.None);

            HandleCompletionWaiters();

            Become(Stopped);

            Sender.Tell(new ProjectionsCoordinator.Responses.StopResponse());
        });

        Receive<InternalCommands.Fail>(cmd =>
        {
            _logger.Error(cmd.Cause, "Projection {0} failed", _configuration.Name);

            _killSwitch?.Shutdown();

            _sequencer.Reset(CancellationToken.None);

            HandleCompletionWaiters(cmd.Cause);

            Become(Stopped);
        });

        Receive<ProjectionsCoordinator.Commands.WaitForCompletion>(_ => { _waitingForCompletion.Add(Sender); });

        Receive<InternalCommands.Complete>(_ =>
        {
            _sequencer.Reset(CancellationToken.None);

            HandleCompletionWaiters();

            Become(Completed);
        });

        Receive<ProjectionsCoordinator.Commands.Kill>(_ =>
        {
            _logger.Info("Killing projection {0}", _configuration.Name);

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

        Receive<ProjectionsCoordinator.Commands.Kill>(_ => { Context.Stop(Self); });

        Receive<ProjectionsCoordinator.Commands.Stop>(_ =>
        {
            Become(Stopped);

            Sender.Tell(new ProjectionsCoordinator.Responses.StopResponse());
        });
    }

    private void HandleCompletionWaiters(Exception? error = null)
    {
        foreach (var item in _waitingForCompletion)
            item.Tell(new ProjectionsCoordinator.Responses.WaitForCompletionResponse(error));

        _waitingForCompletion.Clear();
    }

    protected override void PreStart()
    {
        Self.Tell(new ProjectionsCoordinator.Commands.Start());

        base.PreStart();
    }

    protected override void PreRestart(Exception reason, object message)
    {
        _killSwitch?.Shutdown();

        Self.Tell(new ProjectionsCoordinator.Commands.Start());

        base.PreRestart(reason, message);
    }

    protected override void PostStop()
    {
        _killSwitch?.Shutdown();

        base.PostStop();
    }

    private static Source<NotUsed, NotUsed> MaybeCreateRestartSource(
        Func<Source<NotUsed, NotUsed>> createSource,
        RestartSettings? restartSettings)
    {
        return restartSettings != null
            ? RestartSource.OnFailuresWithBackoff(createSource, restartSettings)
            : createSource();
    }
}