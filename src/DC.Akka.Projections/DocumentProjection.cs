using System.Collections.Immutable;
using Akka.Actor;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections;

public class DocumentProjection<TDocument> : ReceiveActor
{
    public static class Commands
    {
        public record ProjectEvents(object Id, IImmutableList<EventWithPosition> Events);
    }

    private readonly ProjectionConfiguration<TDocument> _configuration;
    private readonly string _id;

    public DocumentProjection(string projectionName, string id)
    {
        _id = id;
        
        var configuration = Context.System.GetExtension<ProjectionsApplication>()
            .GetProjectionConfiguration<TDocument>(projectionName);

        _configuration = configuration ?? throw new NoDocumentProjectionException<TDocument>(id);

        Become(NotLoaded);
    }

    private void NotLoaded()
    {
        ReceiveAsync<Commands.ProjectEvents>(async cmd =>
        {
            var document = await _configuration.Storage.LoadDocument<TDocument>(_id);

            await ProjectEvents(document, cmd.Events);
            
            Become(() => Loaded(document));
        });
    }

    private void Loaded(TDocument? document)
    {
        ReceiveAsync<Commands.ProjectEvents>(async cmd =>
        {
            await ProjectEvents(document, cmd.Events);

            if (_configuration.Storage is IRequireImmutable)
            {
                if (!typeof(TDocument).IsAssignableTo(typeof(IAmImmutable)))
                    Become(NotLoaded);
            }
        });
    }

    private async Task ProjectEvents(TDocument? document, IImmutableList<EventWithPosition> events)
    {
        try
        {
            if (!events.Any())
            {
                Sender.Tell(new Messages.Acknowledge(null));
                
                return;
            }
            
            foreach (var evnt in events)
                document = await _configuration.ProjectionsHandler.Handle(document, evnt.Event, evnt.Position ?? 0);

            var sender = Sender;

            var lastPosition = events.Max(x => x.Position);

            await _configuration
                .Storage
                .StoreDocuments(
                    ImmutableList.Create<(ProjectedDocument, Action, Action<Exception?>)>((
                        new ProjectedDocument(_id, document, lastPosition ?? 0),
                        () => sender.Tell(new Messages.Acknowledge(lastPosition)),
                        cause => sender.Tell(new Messages.Reject(cause)))));
        }
        catch (Exception e)
        {
            Sender.Tell(new Messages.Reject(e));
            
            throw;
        }
    }
}