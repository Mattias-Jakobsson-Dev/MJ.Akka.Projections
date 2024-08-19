using System.Collections.Immutable;
using Akka.Actor;

namespace DC.Akka.Projections.Configuration;

public static class ProjectionSetupConfigurationExtensions
{
    public static IHaveConfiguration<ProjectionSystemConfiguration> WithProjection<TId, TDocument>(
        this IHaveConfiguration<ProjectionSystemConfiguration> source,
        IProjection<TId, TDocument> projection,
        Func<IHaveConfiguration<ProjectionInstanceConfiguration>, IHaveConfiguration<ProjectionInstanceConfiguration>>?
            configure = null)
        where TId : notnull where TDocument : notnull
    {
        return source
            .WithModifiedConfig(x => x with
            {
                Projections = x.Projections.SetItem(
                    projection.Name,
                    conf =>
                    {
                        var configuredProjection = (configure ?? (c => c))(new ConfigureProjection(
                                source.ActorSystem,
                                ProjectionInstanceConfiguration.Empty))
                            .Config
                            .MergeWith(conf);

                        return new ProjectionConfiguration<TId, TDocument>(
                            projection,
                            configuredProjection.ProjectionStorage!,
                            configuredProjection.PositionStorage!,
                            conf.ProjectorFactory,
                            configuredProjection.RestartSettings,
                            configuredProjection.StreamConfiguration!,
                            projection.Configure(new SetupProjection<TId, TDocument>()).Build());
                    })
            });
    }
    
    private record ConfigureProjection(ActorSystem ActorSystem, ProjectionInstanceConfiguration Config) 
        : IHaveConfiguration<ProjectionInstanceConfiguration>
    {
        public IHaveConfiguration<ProjectionInstanceConfiguration> WithModifiedConfig(
            Func<ProjectionInstanceConfiguration, ProjectionInstanceConfiguration> modify)
        {
            return this with
            {
                Config = modify(Config)
            };
        }
    }

    private class SetupProjection<TId, TDocument> : ISetupProjection<TId, TDocument>
        where TId : notnull where TDocument : notnull
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
            Func<IProjectionFilterSetup<TDocument, TProjectedEvent>,
                IProjectionFilterSetup<TDocument, TProjectedEvent>>? filter)
        {
            IProjectionFilterSetup<TDocument, TProjectedEvent> projectionFilterSetup
                = ProjectionFilterSetup<TDocument, TProjectedEvent>.Create();

            projectionFilterSetup = filter?.Invoke(projectionFilterSetup) ?? projectionFilterSetup;

            return new SetupProjection<TId, TDocument>(
                _handlers.SetItem(typeof(TProjectedEvent), new Handler(
                    evnt => getId((TProjectedEvent)evnt),
                    (evnt, doc, position) => handler((TProjectedEvent)evnt, doc, position),
                    projectionFilterSetup.Build())),
                _transformers);
        }

        public ISetupProjection<TId, TDocument> On<TProjectedEvent>(
            Func<TProjectedEvent, TId> getId,
            Func<TProjectedEvent, TDocument?, Task<TDocument?>> handler,
            Func<IProjectionFilterSetup<TDocument, TProjectedEvent>,
                IProjectionFilterSetup<TDocument, TProjectedEvent>>? filter)
        {
            return On(
                getId,
                (evnt, doc, _) => handler(evnt, doc),
                filter);
        }

        public ISetupProjection<TId, TDocument> On<TProjectedEvent>(
            Func<TProjectedEvent, TId> getId,
            Func<TProjectedEvent, TDocument?, TDocument?> handler,
            Func<IProjectionFilterSetup<TDocument, TProjectedEvent>,
                IProjectionFilterSetup<TDocument, TProjectedEvent>>? filter)
        {
            return On(
                getId,
                (evnt, doc, _) => handler(evnt, doc),
                filter);
        }

        public ISetupProjection<TId, TDocument> On<TProjectedEvent>(
            Func<TProjectedEvent, TId> getId,
            Func<TProjectedEvent, TDocument?, long, TDocument?> handler,
            Func<IProjectionFilterSetup<TDocument, TProjectedEvent>,
                IProjectionFilterSetup<TDocument, TProjectedEvent>>? filter)
        {
            return On(
                getId,
                (evnt, doc, offset) => Task.FromResult(handler(evnt, doc, offset)),
                filter);
        }

        public ISetupProjection<TId, TDocument> TransformUsing<TEvent>(
            Func<TEvent, IImmutableList<object>> transform)
        {
            return new SetupProjection<TId, TDocument>(
                _handlers,
                _transformers.SetItem(typeof(TEvent), evnt => transform((TEvent)evnt)));
        }

        public IHandleEventInProjection<TDocument> Build()
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
            : IHandleEventInProjection<TDocument>
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

            public DocumentId GetDocumentIdFrom(object evnt)
            {
                var typesToCheck = evnt.GetType().GetInheritedTypes();

                var ids = (from type in typesToCheck
                        where handlers.ContainsKey(type)
                        let handler = handlers[type]
                        where handler.Filter.FilterEvent(evnt)
                        select handler.GetId(evnt))
                    .ToImmutableList();

                return new DocumentId(
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