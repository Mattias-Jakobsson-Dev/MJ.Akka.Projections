using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public class DocumentProjection<TId, TDocument> : ReceiveActor
    where TId : notnull where TDocument : notnull
{
    public static class Commands
    {
        public interface IMessageWithId
        {
            TId Id { get; }
        }

        public record ProjectEvents(TId Id, IImmutableList<EventWithPosition> Events) : IMessageWithId;
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

    private void NotLoaded()
    {
        ReceiveAsync<Commands.ProjectEvents>(async cmd =>
        {
            var document = await _configuration.DocumentStorage.LoadDocument<TDocument>(_id);

            document = await ProjectEvents(document, cmd.Events);

            Become(() => Loaded(document));
        });
    }

    private void Loaded(TDocument? document)
    {
        ReceiveAsync<Commands.ProjectEvents>(async cmd =>
        {
            document = await ProjectEvents(document, cmd.Events);

            Become(() => Loaded(document));
        });
    }

    private async Task<TDocument?> ProjectEvents(TDocument? document, IImmutableList<EventWithPosition> events)
    {
        try
        {
            var result = await Retries
                .Run<(TDocument? document, long? position), Exception>(
                    RunProjections,
                    _configuration.ProjectionStreamConfiguration.MaxProjectionRetries,
                    (retries, exception) => _logger
                        .Warning(
                            exception,
                            "Failed handling {0} events for {1}, retrying (tries: {2})",
                            events.Count,
                            _id,
                            retries));
            
            Sender.Tell(new Messages.Acknowledge(result.position));

            return result.document;
        }
        catch (Exception e)
        {
            Sender.Tell(new Messages.Reject(e));
            
            _logger.Error(e, "Failed handling {0} events for {1}.{2}", events.Count, _configuration.Name, _id);

            return document;
        }

        async Task<(TDocument? document, long? position)> RunProjections()
        {
            var exists = document != null;

            if (!events.Any())
                return (document, null);

            var wasHandled = false;

            foreach (var evnt in events)
            {
                var (projectionResult, hasHandler) =
                    await _configuration.HandleEvent(document, evnt.Event, evnt.Position ?? 0);

                document = (TDocument?)projectionResult;

                wasHandled = wasHandled || hasHandler;
            }

            if (!wasHandled) 
                return (document, events.Select(x => x.Position).Max());
            
            if (document != null)
            {
                await _configuration
                    .DocumentStorage
                    .Store(
                        ImmutableList.Create(
                            new DocumentToStore(_id, document)),
                        ImmutableList<DocumentToDelete>.Empty);

                if (document is IResetDocument<TDocument> reset)
                    document = reset.Reset();
            }
            else if (exists)
            {
                await _configuration
                    .DocumentStorage
                    .Store(
                        ImmutableList<DocumentToStore>.Empty,
                        ImmutableList.Create(new DocumentToDelete(_id, typeof(TDocument))));
            }

            return (document, events.Select(x => x.Position).Max());
        }
    }
    
    public static Props Init(string projectionName, TId id)
    {
        return Props.Create(() => new DocumentProjection<TId, TDocument>(projectionName, id));
    }
}