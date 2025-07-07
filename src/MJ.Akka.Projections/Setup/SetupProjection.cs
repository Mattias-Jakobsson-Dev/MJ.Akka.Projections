using System.Collections.Immutable;
using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Setup;

internal class SetupProjection<TId, TContext> : ISetupProjection<TId, TContext>
    where TId : notnull where TContext : IProjectionContext
{
    private IImmutableDictionary<Type, HandlerBuilder<TId, TContext>> _handlers;

    private readonly IImmutableDictionary<Type, Func<object, IImmutableList<object>>> _transformers;

    public SetupProjection()
        : this(
            ImmutableDictionary<Type, HandlerBuilder<TId, TContext>>.Empty,
            ImmutableDictionary<Type, Func<object, IImmutableList<object>>>.Empty)
    {
    }

    private SetupProjection(
        IImmutableDictionary<Type, HandlerBuilder<TId, TContext>> handlers,
        IImmutableDictionary<Type, Func<object, IImmutableList<object>>> transformers)
    {
        _handlers = handlers;
        _transformers = transformers;
    }

    public ISetupProjection<TId, TContext> TransformUsing<TEvent>(
        Func<TEvent, IImmutableList<object>> transform)
    {
        return new SetupProjection<TId, TContext>(
            _handlers,
            _transformers.SetItem(typeof(TEvent), evnt => transform((TEvent)evnt)));
    }

    public ISetupEventHandlerForProjection<TId, TContext, TEvent> On<TEvent>(
        Func<TEvent, TId> getId,
        Func<
            IProjectionFilterSetup<TId, TContext, TEvent>,
            IProjectionFilterSetup<TId, TContext, TEvent>>? filter = null)
    {
        IProjectionFilterSetup<TId, TContext, TEvent> projectionFilterSetup
            = ProjectionFilterSetup<TId, TContext, TEvent>.Create();

        projectionFilterSetup = filter?.Invoke(projectionFilterSetup) ?? projectionFilterSetup;

        var builder = new HandlerBuilder<TId, TContext, TEvent>(
            getId,
            projectionFilterSetup.Build(),
            this);
        
        _handlers = _handlers.SetItem(typeof(TEvent), builder);
        
        return builder;
    }

    public IHandleEventInProjection<TId, TContext> Build()
    {
        return new EventHandler(
            _handlers.ToImmutableDictionary(
                x => x.Key,
                x => x.Value.Build()),
            _transformers);
    }

    private class EventHandler(
        IImmutableDictionary<Type, HandlerBuilder<TId, TContext>.Handler> handlers,
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

        public async Task<(bool handled, IImmutableList<IProjectionResult> results)> Handle(
            TContext context,
            object evnt,
            long position,
            CancellationToken cancellationToken)
        {
            var typesToCheck = evnt.GetType().GetInheritedTypes();

            var handled = false;

            var results = new List<IProjectionResult>();

            foreach (var type in typesToCheck)
            {
                if (!handlers.TryGetValue(type, out var handler))
                    continue;

                if (!handler.Filter.FilterResult(context))
                    return (handled, results.ToImmutableList());

                results.AddRange(await handler.Handle(evnt, context, position, cancellationToken));

                handled = true;
            }

            return (handled, results.ToImmutableList());
        }
    }
}