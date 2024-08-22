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
        var documentToProject = document is IResetDocument<TDocument> reset ? reset.Reset() : document;
        
        try
        {
            var result = await RunProjections();
            
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
            var exists = documentToProject != null;

            if (!events.Any())
                return (documentToProject, null);

            var wasHandled = false;

            foreach (var evnt in events)
            {
                var (projectionResult, hasHandler) =
                    await _configuration.HandleEvent(documentToProject, evnt.Event, evnt.Position ?? 0);

                documentToProject = (TDocument?)projectionResult;

                wasHandled = wasHandled || hasHandler;
            }

            if (!wasHandled) 
                return (documentToProject, events.Select(x => x.Position).Max());
            
            if (documentToProject != null)
            {
                await _configuration
                    .DocumentStorage
                    .Store(
                        ImmutableList.Create(
                            new DocumentToStore(_id, documentToProject)),
                        ImmutableList<DocumentToDelete>.Empty);
            }
            else if (exists)
            {
                await _configuration
                    .DocumentStorage
                    .Store(
                        ImmutableList<DocumentToStore>.Empty,
                        ImmutableList.Create(new DocumentToDelete(_id, typeof(TDocument))));
            }

            return (documentToProject, events.Select(x => x.Position).Max());
        }
    }
    
    public static Props Init(string projectionName, TId id)
    {
        return Props.Create(() => new DocumentProjection<TId, TDocument>(projectionName, id));
    }
}