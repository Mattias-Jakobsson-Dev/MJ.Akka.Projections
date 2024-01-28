using System.Collections.Immutable;
using Akka.Actor;
using Akka.Streams;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public class ProjectionConfigurationSetup<TDocument>(
    IProjection<TDocument> projection,
    ActorSystem actorSystem) : IProjectionStorageConfigurationSetup<TDocument>
{
    private bool _autoStart;

    private Func<object, Task<IActorRef>>? _projectionRefFactory;

    private Func<Task<IActorRef>>? _projectionCoordinatorFactory;

    private IProjectionStorage _storage = new InMemoryProjectionStorage();

    private ProjectionStreamConfiguration _projectionStreamConfiguration =
        ProjectionStreamConfiguration.Default;
    
    private RestartSettings _restartSettings = RestartSettings
        .Create(TimeSpan.FromSeconds(3), TimeSpan.FromMinutes(1), 0.2)
        .WithMaxRestarts(5, TimeSpan.FromMinutes(15));

    public ActorSystem System => actorSystem;
    public IProjection<TDocument> Projection => projection;

    public IProjectionConfigurationSetup<TDocument> AutoStart()
    {
        _autoStart = true;

        return this;
    }

    public IProjectionConfigurationSetup<TDocument> WithCoordinatorFactory(Func<Task<IActorRef>> factory)
    {
        _projectionCoordinatorFactory = factory;

        return this;
    }

    public IProjectionConfigurationSetup<TDocument> WithProjectionFactory(Func<object, Task<IActorRef>> factory)
    {
        _projectionRefFactory = factory;

        return this;
    }

    public IProjectionConfigurationSetup<TDocument> WithRestartSettings(RestartSettings restartSettings)
    {
        _restartSettings = restartSettings;

        return this;
    }

    public IProjectionConfigurationSetup<TDocument> WithProjectionStreamConfiguration(
        ProjectionStreamConfiguration projectionStreamConfiguration)
    {
        _projectionStreamConfiguration = projectionStreamConfiguration;

        return this;
    }

    public IProjectionStorageConfigurationSetup<TDocument> WithStorage(IProjectionStorage storage)
    {
        _storage = storage;

        return this;
    }
    
    public IProjectionStorageConfigurationSetup<TDocument> Batched(
        (int Number, TimeSpan Timeout)? batching = null,
        int parallelism = 10)
    {
        if (_storage is BatchedProjectionStorage)
            return this;

        _storage = new BatchedProjectionStorage(
            _storage,
            System,
            batching ?? (100, TimeSpan.FromSeconds(5)),
            parallelism);

        return this;
    }
    
    public async Task<ProjectionsCoordinator<TDocument>.Proxy> Build()
    {
        var setup = new SetupProjection();

        var eventHandler = Projection.Configure(setup).Build();

        if (_projectionRefFactory == null)
        {
            var documentProjectionCoordinator =
                System.ActorOf(Props.Create(() => new InProcDocumentProjectionCoordinator(Projection.Name)));
            
            _projectionRefFactory = async name =>
            {
                var response = await documentProjectionCoordinator
                    .Ask<InProcDocumentProjectionCoordinator.Responses.GetProjectionRefResponse>(
                        new InProcDocumentProjectionCoordinator.Queries.GetProjectionRef(name));
                
                return response.ProjectionRef ?? throw new NoDocumentProjectionException<TDocument>(name);
            };
        }

        if (!System.HasExtension<ProjectionsApplication>())
            System.RegisterExtension(new ProjectionsApplication());
        
        var application = System.WithExtension<ProjectionsApplication>();

        application.WithProjection(Projection.Name, new ProjectionConfiguration<TDocument>(
            Projection.Name,
            _autoStart,
            _storage,
            eventHandler,
            Projection.StartSource,
            _projectionRefFactory,
            _restartSettings,
            _projectionStreamConfiguration));

        _projectionCoordinatorFactory ??= () =>
        {
            return Task.FromResult(System.ActorOf(
                Props.Create(() => new ProjectionsCoordinator<TDocument>(Projection.Name)), Projection.Name));
        };
        
        var coordinator = await _projectionCoordinatorFactory();

        return new ProjectionsCoordinator<TDocument>.Proxy(coordinator);
    }

    private class SetupProjection : ISetupProjection<TDocument>
    {
        private readonly IImmutableDictionary<Type, (Func<object, object> GetId,
            Func<object, TDocument?, long, Task<TDocument?>> Handle)> _handlers;

        public SetupProjection()
            : this(
                ImmutableDictionary<Type, (Func<object, object>, Func<object, TDocument?, long, Task<TDocument?>>)>
                    .Empty)
        {
        }

        private SetupProjection(
            IImmutableDictionary<Type, (Func<object, object>, Func<object, TDocument?, long, Task<TDocument?>>)>
                handlers)
        {
            _handlers = handlers;
        }

        public ISetupProjection<TDocument> RegisterHandler<TProjectedEvent>(
            Func<TProjectedEvent, object> getId,
            Func<TProjectedEvent, TDocument?, long, Task<TDocument?>> handler)
        {
            return new SetupProjection(
                _handlers.SetItem(typeof(TProjectedEvent), (
                    evnt => getId((TProjectedEvent)evnt),
                    (evnt, doc, position) => handler((TProjectedEvent)evnt, doc, position))));
        }

        public ISetupProjection<TDocument> RegisterHandler<TProjectedEvent>(
            Func<TProjectedEvent, object> getId,
            Func<TProjectedEvent, TDocument?, Task<TDocument?>> handler)
        {
            return RegisterHandler(
                getId,
                (evnt, doc, _) => handler(evnt, doc));
        }

        public ISetupProjection<TDocument> RegisterHandler<TProjectedEvent>(
            Func<TProjectedEvent, object> getId,
            Func<TProjectedEvent, TDocument?, TDocument?> handler)
        {
            return RegisterHandler(
                getId,
                (evnt, doc, _) => handler(evnt, doc));
        }

        public ISetupProjection<TDocument> RegisterHandler<TProjectedEvent>(
            Func<TProjectedEvent, object> getId,
            Func<TProjectedEvent, TDocument?, long, TDocument?> handler)
        {
            return RegisterHandler(
                getId,
                (evnt, doc, offset) => Task.FromResult(handler(evnt, doc, offset)));
        }

        public IHandleEventInProjection<TDocument> Build()
        {
            return new EventHandler(_handlers);
        }

        private class EventHandler(
            IImmutableDictionary<Type, (Func<object, object>, Func<object, TDocument?, long, Task<TDocument?>>)>
                handlers)
            : IHandleEventInProjection<TDocument>
        {
            private readonly IImmutableDictionary<Type, (Func<object, object> GetId,
                Func<object, TDocument?, long, Task<TDocument?>> Handle)> _handlers = handlers;

            public object? GetDocumentIdFrom(object evnt)
            {
                var typesToCheck = evnt.GetType().GetInheritedTypes();

                return (from type in typesToCheck
                        where _handlers.ContainsKey(type)
                        select _handlers[type].GetId(evnt))
                    .FirstOrDefault();
            }

            public async Task<TDocument?> Handle(TDocument? document, object evnt, long position)
            {
                var typesToCheck = evnt.GetType().GetInheritedTypes();

                foreach (var type in typesToCheck)
                {
                    if (!_handlers.ContainsKey(type))
                        continue;

                    document = await _handlers[type].Handle(evnt, document, position);
                }

                return document;
            }
        }
    }
    
    private class InProcDocumentProjectionCoordinator : ReceiveActor
    {
        public static class Queries
        {
            public record GetProjectionRef(object Id);
        }
    
        public static class Responses
        {
            public record GetProjectionRefResponse(IActorRef? ProjectionRef);
        }

        public InProcDocumentProjectionCoordinator(string projectionName)
        {
            Receive<Queries.GetProjectionRef>(cmd =>
            {
                var id = cmd.Id.ToString();

                if (string.IsNullOrEmpty(id))
                {
                    Sender.Tell(new Responses.GetProjectionRefResponse(null));
                    
                    return;
                }
                
                var projectionRef = Context.Child(id);
            
                if (projectionRef.IsNobody())
                    projectionRef = Context.ActorOf(Props.Create(() => new DocumentProjection<TDocument>(projectionName, id)), id);
            
                Sender.Tell(new Responses.GetProjectionRefResponse(projectionRef));
            });
        }
    }
}