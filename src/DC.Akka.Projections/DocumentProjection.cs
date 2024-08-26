using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public class DocumentProjection<TId, TDocument> : ReceiveActor, IWithStash
    where TId : notnull where TDocument : notnull
{
    public static class Commands
    {
        public interface IMessageWithId
        {
            TId Id { get; }
        }

        public record ProjectEvents(TId Id, ImmutableList<EventWithPosition> Events) : IMessageWithId;

        public record StopInProcessEvents(TId Id) : IMessageWithId;
    }

    private readonly ProjectionConfiguration _configuration;
    private readonly TId _id;
    private readonly ILoggingAdapter _logger;

    public DocumentProjection(string projectionName, TId id)
    {
        _id = id;
        _logger = Context.GetLogger();

        _configuration = Context
                             .System
                             .GetExtension<ProjectionConfigurationsSupplier>()?
                             .GetConfigurationFor(projectionName) ??
                         throw new NoDocumentProjectionException<TDocument>(projectionName);

        Become(NotLoaded);
    }

    public IStash Stash { get; set; } = null!;

    private void NotLoaded()
    {
        Receive<Commands.ProjectEvents>(cmd =>
        {
            ProjectEvents(cmd.Events, () => _configuration.DocumentStorage.LoadDocument<TDocument>(_id));
        });

        Receive<Commands.StopInProcessEvents>(_ =>
        {
            var waitingItems = Stash.ClearStash();

            foreach (var waitingItem in waitingItems)
            {
                waitingItem
                    .Sender
                    .Tell(new Messages.Reject(new Exception("Projection stopped")));
            }
        });
    }

    private void Loaded(TDocument? document)
    {
        Receive<Commands.ProjectEvents>(cmd =>
        {
            ProjectEvents(cmd.Events, () => Task.FromResult(document));
        });

        Receive<Commands.StopInProcessEvents>(_ =>
        {
            var waitingItems = Stash.ClearStash();

            foreach (var waitingItem in waitingItems)
            {
                waitingItem
                    .Sender
                    .Tell(new Messages.Reject(new Exception("Projection stopped")));
            }

            Become(NotLoaded);
        });
    }

    private void ProcessingEvents(IActorRef from, CancellationTokenSource cancellation)
    {
        Receive<Commands.ProjectEvents>(_ => { Stash.Stash(); });

        Receive<ProjectionResponse>(cmd =>
        {
            from.Tell(cmd.Response);

            Stash.UnstashAll();

            if (cmd.Response is Messages.Acknowledge)
                Become(() => Loaded(cmd.Document));
            else
                Become(NotLoaded);
        });

        ReceiveAsync<Commands.StopInProcessEvents>(async _ =>
        {
            await cancellation.CancelAsync();

            var waitingItems = Stash.ClearStash();

            foreach (var waitingItem in waitingItems)
            {
                waitingItem
                    .Sender
                    .Tell(new Messages.Reject(new Exception("Projection stopped")));
            }

            Become(NotLoaded);
        });
    }

    private void ProjectEvents(
        ImmutableList<EventWithPosition> events,
        Func<Task<TDocument?>> loadDocument)
    {
        var cancellation = new CancellationTokenSource();

        StartProjectingEvents(
                loadDocument,
                events,
                cancellation.Token)
            .PipeTo(Self);

        Become(() => ProcessingEvents(Sender, cancellation));
    }

    private async Task<ProjectionResponse> StartProjectingEvents(
        Func<Task<TDocument?>> loadDocument,
        ImmutableList<EventWithPosition> events,
        CancellationToken cancellationToken)
    {
        TDocument? document;

        try
        {
            document = await loadDocument();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed loading document for {0}.{1}", _configuration.Name, _id);

            return new ProjectionResponse(default, new Messages.Reject(e));
        }

        var documentToProject = document is IResetDocument<TDocument> reset ? reset.Reset() : document;

        try
        {
            var result = await RunProjections();

            return new ProjectionResponse(result.document, new Messages.Acknowledge(result.position));
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed handling {0} events for {1}.{2}", events.Count, _configuration.Name, _id);

            return new ProjectionResponse(document, new Messages.Reject(e));
        }

        async Task<(TDocument? document, long? position)> RunProjections()
        {
            var exists = documentToProject != null;

            if (events.IsEmpty)
                return (documentToProject, null);

            var wasHandled = false;

            foreach (var evnt in events.OrderBy(x => x.Position ?? 0))
            {
                var (projectionResult, hasHandler) =
                    await _configuration.HandleEvent(documentToProject, evnt.Event, evnt.Position ?? 0, cancellationToken);

                documentToProject = (TDocument?)projectionResult;

                wasHandled = wasHandled || hasHandler;
            }

            if (!wasHandled)
                return (documentToProject, events.GetHighestEventNumber());

            if (documentToProject != null)
            {
                await _configuration
                    .DocumentStorage
                    .Store(
                        ImmutableList.Create(
                            new DocumentToStore(_id, documentToProject)),
                        ImmutableList<DocumentToDelete>.Empty,
                        cancellationToken);
            }
            else if (exists)
            {
                await _configuration
                    .DocumentStorage
                    .Store(
                        ImmutableList<DocumentToStore>.Empty,
                        ImmutableList.Create(new DocumentToDelete(_id, typeof(TDocument))),
                        cancellationToken);
            }

            return (documentToProject, events.GetHighestEventNumber());
        }
    }

    public static Props Init(string projectionName, TId id)
    {
        return Props.Create(() => new DocumentProjection<TId, TDocument>(projectionName, id));
    }

    private record ProjectionResponse(TDocument? Document, Messages.IProjectEventsResponse Response);
}