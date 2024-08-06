using System.Collections.Immutable;
using Akka.Actor;
using DC.Akka.Projections.Configuration;

namespace DC.Akka.Projections;

public class DocumentProjection<TId, TDocument> : ReceiveActor, IWithTimers 
    where TId : notnull where TDocument : notnull
{
    public static class Commands
    {
        public record ProjectEvents(object Id, IImmutableList<EventWithPosition> Events);
    }

    private readonly ProjectionConfiguration<TId, TDocument> _configuration;
    private readonly string _projectionName;
    private readonly TId _id;
    private readonly TimeSpan? _passivateAfter;
    
    public ITimerScheduler? Timers { get; set; }

    public DocumentProjection(string projectionName, TId id, TimeSpan? passivateAfter)
    {
        _projectionName = projectionName;
        _id = id;
        _passivateAfter = passivateAfter;
        
        var configuration = Context.System.GetExtension<ProjectionsApplication>()
            .GetProjectionConfiguration<TId, TDocument>(projectionName);

        _configuration = configuration ?? throw new NoDocumentProjectionException<TId, TDocument>(id);

        Become(NotLoaded);

        HandlePassivation();
    }

    private void NotLoaded()
    {
        ReceiveAsync<Commands.ProjectEvents>(async cmd =>
        {
            var (document, requireReload) = await _configuration.DocumentStorage.LoadDocument(_id);

            document = await ProjectEvents(document, cmd.Events);
            
            if (!requireReload)
                Become(() => Loaded(document));
            
            HandlePassivation();
        });
    }

    private void Loaded(TDocument? document)
    {
        ReceiveAsync<Commands.ProjectEvents>(async cmd =>
        {
            document = await ProjectEvents(document, cmd.Events);
            
            Become(() => Loaded(document));
            
            HandlePassivation();
        });
    }

    private async Task<TDocument?> ProjectEvents(TDocument? document, IImmutableList<EventWithPosition> events)
    {
        var exists = document != null;
        
        try
        {
            if (!events.Any())
            {
                Sender.Tell(new Messages.Acknowledge());
                
                return document;
            }
            
            foreach (var evnt in events)
                document = await _configuration.ProjectionsHandler.Handle(document, evnt.Event, evnt.Position ?? 0);

            if (document != null)
            {
                await _configuration
                    .StorageSession
                    .Store(_projectionName, _id, document, Sender);
            }
            else if (exists)
            {
                await _configuration
                    .StorageSession
                    .Delete<TId, TDocument>(_projectionName, _id, Sender);
            }

            return document;
        }
        catch (Exception e)
        {
            Sender.Tell(new Messages.Reject(e));
            
            throw;
        }
    }

    private void HandlePassivation()
    {
        if (_passivateAfter == null || Timers == null)
            return;
        
        Timers.StartSingleTimer(
            "passivation",
            PoisonPill.Instance,
            _passivateAfter.Value);
    }
}