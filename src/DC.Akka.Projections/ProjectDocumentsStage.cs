using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Stage;
using Akka.Streams.Supervision;
using DC.Akka.Projections.Configuration;
using Decider = Akka.Streams.Supervision.Decider;
using Directive = Akka.Streams.Supervision.Directive;

namespace DC.Akka.Projections;

internal class ProjectDocumentsStage<TId, TDocument> : GraphStage<FlowShape<IImmutableList<EventWithPosition>, long?>>
    where TId : notnull where TDocument : notnull
{
    private readonly int _parallelism;
    private readonly ProjectionConfiguration<TId, TDocument> _configuration;

    private readonly Inlet<IImmutableList<EventWithPosition>> _in = new("ProjectDocumentsStage.in");

    private readonly Outlet<long?> _out = new("ProjectDocumentsStage.out");
    
    protected override Attributes InitialAttributes { get; } = Attributes.CreateName("ProjectDocumentsStage");

    public ProjectDocumentsStage(int parallelism, ProjectionConfiguration<TId, TDocument> configuration)
    {
        _parallelism = parallelism;
        _configuration = configuration;

        Shape = new FlowShape<IImmutableList<EventWithPosition>, long?>(_in, _out);
    }

    public override FlowShape<IImmutableList<EventWithPosition>, long?> Shape { get; }

    protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes)
    {
        return new Logic(inheritedAttributes, this);
    }
    
    public override string ToString() => "ProjectDocumentsStage";

    private sealed class Logic : InAndOutGraphStageLogic
    {
        private readonly ProjectDocumentsStage<TId, TDocument> _stage;
        private readonly Decider _decider;
        
        private BlockingUniqueQueue<Task<Messages.IProjectEventsResponse>> _queue = null!;

        public Logic(Attributes inheritedAttributes, ProjectDocumentsStage<TId, TDocument> stage) : base(stage.Shape)
        {
            _stage = stage;
            var attr = inheritedAttributes.GetAttribute<ActorAttributes.SupervisionStrategy>(null!);
            _decider = attr != null ? attr.Decider : Deciders.StoppingDecider;

            SetHandlers(stage._in, stage._out, this);
        }

        public override void OnPush()
        {
            var message = Grab(_stage._in);

            try
            {
                var messagesPerProjection = message
                    .SelectMany(x => _stage._configuration
                        .ProjectionsHandler
                        .Transform(x.Event)
                        .Select(y => x with { Event = y }))
                    .Select(x => new
                    {
                        Event = x,
                        Id = _stage._configuration.ProjectionsHandler.GetDocumentIdFrom(x.Event)
                    })
                    .GroupBy(x => x.Id)
                    .Select(x => (
                        Events: x.Select(y => y.Event).ToImmutableList(),
                        Id: x.Key))
                    .Select(x => new
                    {
                        x.Events,
                        x.Id,
                        LowestPosition = x.Events.Count > 0 ? x.Events.Min(y => y.Position) : null
                    })
                    .OrderBy(y => y.LowestPosition)
                    .Select(x => new
                    {
                        x.Events,
                        x.Id
                    })
                    .ToImmutableList();

                foreach (var perProjection in messagesPerProjection)
                {
                    _queue
                        .Enqueue(
                            perProjection.Id.Id != null ? perProjection.Id.Id : new object(),
                            async () =>
                            {
                                if (perProjection.Events.IsEmpty)
                                    return new Messages.Acknowledge(null);

                                if (!perProjection.Id.HasMatch || perProjection.Id.Id == null)
                                    return new Messages.Acknowledge(perProjection.Events.Select(x => x.Position).Max());

                                var projectionRef =
                                    await _stage._configuration.CreateProjectionRef(perProjection.Id.Id);

                                return await Retries
                                    .Run<Messages.IProjectEventsResponse, AskTimeoutException>(
                                        () => projectionRef
                                            .Ask<Messages.IProjectEventsResponse>(
                                                new DocumentProjection<TId, TDocument>.Commands.ProjectEvents(
                                                    perProjection.Id,
                                                    perProjection.Events),
                                                _stage._configuration.ProjectionStreamConfiguration
                                                    .ProjectDocumentTimeout),
                                        _stage._configuration.ProjectionStreamConfiguration.MaxProjectionRetries,
                                        (retries, exception) => Log
                                            .Warning(
                                                exception,
                                                "Failed handling {0} events for {1}, retrying (tries: {2})",
                                                perProjection.Events.Count,
                                                perProjection.Id,
                                                retries));
                            });
                }
            }
            catch (Exception e)
            {
                var strategy = _decider(e);
                Log.Error(e,
                    "An exception occured inside ProjectDocumentsStage while processing message [{0}]. Supervision strategy: {1}",
                    message, strategy);
                
                switch (strategy)
                {
                    case Directive.Stop:
                        FailStage(e);
                        break;

                    case Directive.Resume:
                    case Directive.Restart:
                        break;

                    default:
                        throw new AggregateException($"Unknown SupervisionStrategy directive: {strategy}", e);
                }
            }
            
            var (_, hasCapacity) = _queue.TryPeek(out _);

            if (hasCapacity && !HasBeenPulled(_stage._in))
                TryPull(_stage._in);
        }
        
        public override void OnUpstreamFinish()
        {
            if (_queue.IsEmpty)
                CompleteStage();
        }
        
        public override void PreStart() => _queue = new BlockingUniqueQueue<Task<Messages.IProjectEventsResponse>>(_stage._parallelism);

        public override void OnPull() => PushOne();

        private void PushOne()
        {
            var inlet = _stage._in;

            while (true)
            {
                var (hasItems, hasCapacity) = _queue.TryPeek(out var item);

                if (!hasItems || item == null)
                {
                    if (IsClosed(inlet))
                        CompleteStage();
                    else if (!HasBeenPulled(inlet))
                        Pull(inlet);
                }
                else if (!item.IsCompleted)
                {
                    if (hasCapacity && !HasBeenPulled(inlet))
                        TryPull(inlet);

                    continue;
                }
                else
                {
                    var response = _queue.Dequeue();

                    if (!response.IsCompletedSuccessfully)
                        FailStage(response.Exception);

                    var success = false;

                    switch (response.Result)
                    {
                        case Messages.Acknowledge ack:
                            Push(_stage._out, ack.Position);

                            success = true;
                            break;
                        case Messages.Reject nack:
                            FailStage(new Exception("Rejected projection", nack.Cause));

                            break;
                        default:
                            FailStage(new Exception("Unknown projection response"));

                            break;
                    }

                    if (success && hasCapacity && !HasBeenPulled(inlet))
                        TryPull(inlet);
                }

                break;
            }
        }
    }
}