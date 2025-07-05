using System.Collections.Immutable;

namespace MJ.Akka.Projections.Configuration;

internal class SetupProjection<TId, TContext> : ISetupProjection<TId, TContext>
    where TId : notnull where TContext : IProjectionContext<TId>
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

    public ISetupProjection<TId, TContext> On<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TContext, long, Task> handler,
        Func<IProjectionFilterSetup<TId, TContext, TEvent>,
            IProjectionFilterSetup<TId, TContext, TEvent>>? filter)
    {
        return On(
            getId, 
            (evnt, ctx, position, _) => handler(evnt, ctx, position),
            filter);
    }

    public ISetupProjection<TId, TContext> On<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TContext, CancellationToken, Task> handler,
        Func<IProjectionFilterSetup<TId, TContext, TEvent>, IProjectionFilterSetup<TId, TContext, TEvent>>? filter = null)
    {
        return On(
            getId, 
            (evnt, ctx, _, cancellationToken) => handler(evnt, ctx, cancellationToken),
            filter);
    }

    public ISetupProjection<TId, TContext> On<TEvent>(
        Func<TEvent, TId> getId, 
        Func<TEvent, TContext, long, CancellationToken, Task> handler,
        Func<IProjectionFilterSetup<TId, TContext, TEvent>, IProjectionFilterSetup<TId, TContext, TEvent>>? filter = null)
    {
        IProjectionFilterSetup<TId, TContext, TEvent> projectionFilterSetup
            = ProjectionFilterSetup<TId, TContext, TEvent>.Create();

        projectionFilterSetup = filter?.Invoke(projectionFilterSetup) ?? projectionFilterSetup;

        return new SetupProjection<TId, TContext>(
            _handlers.SetItem(typeof(TEvent), new Handler(
                evnt => getId((TEvent)evnt),
                (evnt, ctx, position, cancellationToken) =>
                    handler((TEvent)evnt, ctx, position, cancellationToken),
                projectionFilterSetup.Build())),
            _transformers);
    }

    public ISetupProjection<TId, TContext> On<TProjectedEvent>(
        Func<TProjectedEvent, TId> getId,
        Func<TProjectedEvent, TContext, Task> handler,
        Func<IProjectionFilterSetup<TId, TContext, TProjectedEvent>,
            IProjectionFilterSetup<TId, TContext, TProjectedEvent>>? filter)
    {
        return On(
            getId,
            (evnt, ctx, _, _) => handler(evnt, ctx),
            filter);
    }

    public ISetupProjection<TId, TContext> On<TProjectedEvent>(
        Func<TProjectedEvent, TId> getId,
        Action<TProjectedEvent, TContext> handler,
        Func<IProjectionFilterSetup<TId, TContext, TProjectedEvent>,
            IProjectionFilterSetup<TId, TContext, TProjectedEvent>>? filter)
    {
        return On(
            getId,
            (evnt, ctx, _) => handler(evnt, ctx),
            filter);
    }

    public ISetupProjection<TId, TContext> On<TProjectedEvent>(
        Func<TProjectedEvent, TId> getId,
        Action<TProjectedEvent, TContext, long> handler,
        Func<IProjectionFilterSetup<TId, TContext, TProjectedEvent>,
            IProjectionFilterSetup<TId, TContext, TProjectedEvent>>? filter)
    {
        return On(
            getId,
            (evnt, ctx, offset) =>
            {
                handler(evnt, ctx, offset);

                return Task.CompletedTask;
            },
            filter);
    }

    public ISetupProjection<TId, TContext> TransformUsing<TEvent>(
        Func<TEvent, IImmutableList<object>> transform)
    {
        return new SetupProjection<TId, TContext>(
            _handlers,
            _transformers.SetItem(typeof(TEvent), evnt => transform((TEvent)evnt)));
    }

    public IHandleEventInProjection<TId, TContext> Build()
    {
        return new EventHandler(_handlers, _transformers);
    }

    private record Handler(
        Func<object, TId> GetId,
        Func<object, TContext, long, CancellationToken, Task> Handle,
        IProjectionFilter<TContext> Filter);

    private class EventHandler(
        IImmutableDictionary<Type, Handler> handlers,
        IImmutableDictionary<Type, Func<object, IImmutableList<object>>> transformers)
        : IHandleEventInProjection<TId, TContext>
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

        public async Task<bool> Handle(
            TContext context,
            object evnt,
            long position,
            CancellationToken cancellationToken)
        {
            var typesToCheck = evnt.GetType().GetInheritedTypes();

            var handled = false;

            foreach (var type in typesToCheck)
            {
                if (!handlers.TryGetValue(type, out var handler))
                    continue;

                if (!handler.Filter.FilterResult(context))
                    return handled;

                await handler.Handle(evnt, context, position, cancellationToken);

                handled = true;
            }

            return handled;
        }
    }
}