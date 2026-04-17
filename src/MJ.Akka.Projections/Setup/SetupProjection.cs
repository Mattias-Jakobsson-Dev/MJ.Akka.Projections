using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

internal class SetupProjection<TIdContext, TContext> : ISetupProjection<TIdContext, TContext>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    private IImmutableDictionary<Type, HandlerFilteringBuilderBase<TIdContext, TContext>> _builders =
        ImmutableDictionary<Type, HandlerFilteringBuilderBase<TIdContext, TContext>>.Empty;

    private IImmutableDictionary<Type, Func<object, IImmutableList<object>>> _transformers =
        ImmutableDictionary<Type, Func<object, IImmutableList<object>>>.Empty;

    public ISetupEventRouting<TIdContext, TContext, TEvent> On<TEvent>()
        => new EventRoutingBuilder<TIdContext, TContext, TEvent>(this);

    internal ISetupProjection<TIdContext, TContext> RegisterTransformer<TEvent>(
        Func<TEvent, IImmutableList<object>> transform)
    {
        _transformers = _transformers.SetItem(typeof(TEvent), evnt => transform((TEvent)evnt));
        return this;
    }

    internal ISetupHandlerFiltering<TIdContext, TContext, TEvent> GetOrCreateHandlerBuilder<TEvent>(
        Func<TEvent, Task<TIdContext?>> getId)
    {
        var builder = new HandlerFilteringBuilder<TIdContext, TContext, TEvent>(getId, this);
        _builders = _builders.SetItem(typeof(TEvent), builder);
        return builder;
    }

    public IHandleEventInProjection<TIdContext, TContext> Build()
    {
        return new EventHandler(
            _builders.ToImmutableDictionary(x => x.Key, x => x.Value.BuildHandler()),
            _transformers);
    }

    private class EventHandler(
        IImmutableDictionary<Type, HandlerFilteringBuilderBase<TIdContext, TContext>.Handler> handlers,
        IImmutableDictionary<Type, Func<object, IImmutableList<object>>> transformers)
        : IHandleEventInProjection<TIdContext, TContext>
    {
        public IImmutableList<object> Transform(object evnt)
        {
            var typesToCheck = evnt.GetType().GetInheritedTypes();
            var results = ImmutableList.Create(evnt);

            foreach (var type in typesToCheck)
            {
                if (!transformers.ContainsKey(type))
                    continue;

                results = results.AddRange(transformers[type](evnt));
            }

            return results;
        }

        public async Task<TIdContext?> GetIdContextFor(object evnt)
        {
            var typesToCheck = evnt.GetType().GetInheritedTypes();

            return (await Task.WhenAll(from type in typesToCheck
                    where handlers.ContainsKey(type)
                    let handler = handlers[type]
                    where handler.Filter.FilterEvent(evnt)
                    select handler.GetId(evnt)))
                .FirstOrDefault(x => x != null);
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

                await handler.Handle(evnt, context, position, cancellationToken);

                handled = true;
            }

            return handled;
        }
    }
}