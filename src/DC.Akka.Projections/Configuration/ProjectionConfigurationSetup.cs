using System.Collections.Immutable;
using Akka.Actor;
using Akka.Streams;
using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public class ProjectionConfigurationSetup<TId, TDocument>(
    IProjection<TId, TDocument> projection,
    ActorSystem actorSystem)
    : IProjectionConfigurationSetup<TId, TDocument>
    where TId : notnull where TDocument : notnull
{
    private IKeepTrackOfProjectors? _projectionFactory;

    private IStartProjectionCoordinator? _projectionCoordinatorFactory;

    private IProjectionStorage? _storage;

    private IProjectionPositionStorage? _positionStorage;

    private ProjectionStreamConfiguration? _projectionStreamConfiguration;

    private RestartSettings? _restartSettings;

    public IProjection<TId, TDocument> Projection { get; } = projection;
    public ActorSystem ActorSystem => actorSystem;

    public IProjectionConfigurationSetup<TId, TDocument> WithCoordinatorFactory(IStartProjectionCoordinator factory)
    {
        _projectionCoordinatorFactory = factory;

        return this;
    }

    public IProjectionConfigurationSetup<TId, TDocument> WithProjectionFactory(IKeepTrackOfProjectors factory)
    {
        _projectionFactory = factory;

        return this;
    }

    public IProjectionConfigurationSetup<TId, TDocument> WithRestartSettings(RestartSettings restartSettings)
    {
        _restartSettings = restartSettings;

        return this;
    }

    public IProjectionConfigurationSetup<TId, TDocument> WithProjectionStreamConfiguration(
        ProjectionStreamConfiguration projectionStreamConfiguration)
    {
        _projectionStreamConfiguration = projectionStreamConfiguration;

        return this;
    }

    public IProjectionConfigurationSetup<TId, TDocument> WithProjectionStorage(IProjectionStorage storage)
    {
        _storage = storage;

        return this;
    }

    public IProjectionConfigurationSetup<TId, TDocument> WithPositionStorage(IProjectionPositionStorage positionStorage)
    {
        _positionStorage = positionStorage;

        return this;
    }

    public ProjectionConfiguration<TId, TDocument> Build(IProjectionsSetup projectionsSetup)
    {
        var setup = new SetupProjection();

        var eventHandler = Projection.Configure(setup).Build();

        var projectionFactory = _projectionFactory ?? projectionsSetup.ProjectionFactory;
        
        var projectionCoordinatorFactory = _projectionCoordinatorFactory ?? projectionsSetup.CoordinatorFactory;

        return new ProjectionConfiguration<TId, TDocument>(
            Projection,
            _storage ?? projectionsSetup.Storage,
            _positionStorage ?? projectionsSetup.PositionStorage,
            eventHandler,
            Projection.StartSource,
            projectionFactory,
            projectionCoordinatorFactory,
            _restartSettings ?? projectionsSetup.RestartSettings,
            _projectionStreamConfiguration ?? projectionsSetup.ProjectionStreamConfiguration);
    }

    private class SetupProjection : ISetupProjection<TId, TDocument>
    {
        private readonly IImmutableDictionary<Type, Handler> _handlers;

        private readonly IImmutableDictionary<Type, Func<object, IImmutableList<object>>> _transformers;

        public SetupProjection()
            : this(
                ImmutableDictionary<Type, Handler>.Empty,
                ImmutableDictionary<Type, Func<object, IImmutableList<object>>>.Empty)
        {
        }

        private SetupProjection(
            IImmutableDictionary<Type, Handler> handlers,
            IImmutableDictionary<Type, Func<object, IImmutableList<object>>> transformers)
        {
            _handlers = handlers;
            _transformers = transformers;
        }

        public ISetupProjection<TId, TDocument> On<TProjectedEvent>(
            Func<TProjectedEvent, TId> getId,
            Func<TProjectedEvent, TDocument?, long, Task<TDocument?>> handler,
            Func<IProjectionFilterSetup<TDocument, TProjectedEvent>, IProjectionFilterSetup<TDocument, TProjectedEvent>>? filter)
        {
            IProjectionFilterSetup<TDocument, TProjectedEvent> projectionFilterSetup 
                = ProjectionFilterSetup<TDocument, TProjectedEvent>.Create();

            projectionFilterSetup = filter?.Invoke(projectionFilterSetup) ?? projectionFilterSetup;
            
            return new SetupProjection(
                _handlers.SetItem(typeof(TProjectedEvent), new Handler(
                    evnt => getId((TProjectedEvent)evnt),
                    (evnt, doc, position) => handler((TProjectedEvent)evnt, doc, position),
                    projectionFilterSetup.Build())),
                _transformers);
        }

        public ISetupProjection<TId, TDocument> On<TProjectedEvent>(
            Func<TProjectedEvent, TId> getId,
            Func<TProjectedEvent, TDocument?, Task<TDocument?>> handler,
            Func<IProjectionFilterSetup<TDocument, TProjectedEvent>, IProjectionFilterSetup<TDocument, TProjectedEvent>>? filter)
        {
            return On(
                getId,
                (evnt, doc, _) => handler(evnt, doc),
                filter);
        }

        public ISetupProjection<TId, TDocument> On<TProjectedEvent>(
            Func<TProjectedEvent, TId> getId,
            Func<TProjectedEvent, TDocument?, TDocument?> handler,
            Func<IProjectionFilterSetup<TDocument, TProjectedEvent>, IProjectionFilterSetup<TDocument, TProjectedEvent>>? filter)
        {
            return On(
                getId,
                (evnt, doc, _) => handler(evnt, doc),
                filter);
        }

        public ISetupProjection<TId, TDocument> On<TProjectedEvent>(
            Func<TProjectedEvent, TId> getId,
            Func<TProjectedEvent, TDocument?, long, TDocument?> handler,
            Func<IProjectionFilterSetup<TDocument, TProjectedEvent>, IProjectionFilterSetup<TDocument, TProjectedEvent>>? filter)
        {
            return On(
                getId,
                (evnt, doc, offset) => Task.FromResult(handler(evnt, doc, offset)),
                filter);
        }

        public ISetupProjection<TId, TDocument> TransformUsing<TEvent>(
            Func<TEvent, IImmutableList<object>> transform)
        {
            return new SetupProjection(
                _handlers,
                _transformers.SetItem(typeof(TEvent), evnt => transform((TEvent)evnt)));
        }
        
        public IHandleEventInProjection<TId, TDocument> Build()
        {
            return new EventHandler(_handlers, _transformers);
        }
        
        private record Handler(
            Func<object, TId> GetId,
            Func<object, TDocument?, long, Task<TDocument?>> Handle,
            IProjectionFilter<TDocument> Filter);

        private class EventHandler(
            IImmutableDictionary<Type, Handler> handlers,
            IImmutableDictionary<Type, Func<object, IImmutableList<object>>> transformers)
            : IHandleEventInProjection<TId, TDocument>
        {
            public IImmutableList<object> Transform(object evnt)
            {
                var typesToCheck = evnt.GetType().GetInheritedTypes();
                var results = ImmutableList<object>.Empty;
                var hasTransformed = false;

                foreach (var type in typesToCheck)
                {
                    if (!transformers.ContainsKey(type))
                        continue;

                    results = results.AddRange(transformers[type](evnt));

                    hasTransformed = true;
                }

                return hasTransformed ? results : ImmutableList.Create(evnt);
            }

            public IHandleEventInProjection<TId, TDocument>.DocumentIdResponse GetDocumentIdFrom(object evnt)
            {
                var typesToCheck = evnt.GetType().GetInheritedTypes();

                var ids = (from type in typesToCheck
                        where handlers.ContainsKey(type)
                        let handler = handlers[type]
                        where handler.Filter.FilterEvent(evnt)
                        select handler.GetId(evnt))
                    .ToImmutableList();

                return new IHandleEventInProjection<TId, TDocument>.DocumentIdResponse(
                    ids.FirstOrDefault(),
                    !ids.IsEmpty);
            }

            public async Task<(TDocument? document, bool hasHandler)> Handle(
                TDocument? document,
                object evnt,
                long position)
            {
                var typesToCheck = evnt.GetType().GetInheritedTypes();

                var handled = false;

                foreach (var type in typesToCheck)
                {
                    if (!handlers.TryGetValue(type, out var handler))
                        continue;

                    if (!handler.Filter.FilterDocument(document))
                        return (document, handled);

                    document = await handler.Handle(evnt, document, position);

                    handled = true;
                }

                return (document, handled);
            }
        }
    }
}