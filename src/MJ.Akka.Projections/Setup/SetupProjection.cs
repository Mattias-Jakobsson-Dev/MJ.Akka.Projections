using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

internal class SetupProjection<TIdContext, TContext> : ISetupProjection<TIdContext, TContext>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    private IImmutableDictionary<Type, HandlerFilteringBuilderBase<TIdContext, TContext>> _builders =
        ImmutableDictionary<Type, HandlerFilteringBuilderBase<TIdContext, TContext>>.Empty;

    private IImmutableDictionary<Type, Func<object, Task<IImmutableList<object>>>> _transformers =
        ImmutableDictionary<Type, Func<object, Task<IImmutableList<object>>>>.Empty;

    public ISetupEventRouting<TIdContext, TContext, TEvent> On<TEvent>()
        => new EventRoutingBuilder<TIdContext, TContext, TEvent>(this);

    internal ISetupProjection<TIdContext, TContext> RegisterTransformer<TEvent>(
        Func<TEvent, IImmutableList<object>> transform)
    {
        _transformers = _transformers.SetItem(typeof(TEvent), evnt => Task.FromResult(transform((TEvent)evnt)));
        return this;
    }

    internal ISetupProjection<TIdContext, TContext> RegisterTransformerWithData<TEvent, TData>(
        Func<TEvent, Task<TData>> getData,
        Func<TEvent, TData, IImmutableList<object>> transform)
    {
        _transformers = _transformers.SetItem(typeof(TEvent), async evnt =>
        {
            var data = await getData((TEvent)evnt);
            return transform((TEvent)evnt, data);
        });
        return this;
    }

    internal ISetupHandlerFiltering<TIdContext, TContext, TEvent> GetOrCreateHandlerBuilder<TEvent>(
        Func<TEvent, TIdContext?> getId)
    {
        var builder = new HandlerFilteringBuilder<TIdContext, TContext, TEvent>(getId, this);
        _builders = _builders.SetItem(typeof(TEvent), builder);
        return builder;
    }

    internal ISetupHandlerFiltering<TIdContext, TContext, TEvent, TData> GetOrCreateHandlerBuilderWithData<TEvent, TData>(
        Func<TEvent, Task<TData>> getData,
        Func<TEvent, TData, TIdContext?> getId)
    {
        var builder = new HandlerFilteringBuilderWithData<TIdContext, TContext, TEvent, TData>(getData, getId, this);
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
        IImmutableDictionary<Type, Func<object, Task<IImmutableList<object>>>> transformers)
        : IHandleEventInProjection<TIdContext, TContext>
    {
        public async Task<IImmutableList<object>> Transform(object evnt)
        {
            var typesToCheck = evnt.GetType().GetInheritedTypes();
            var results = ImmutableList.Create(evnt);

            foreach (var type in typesToCheck)
            {
                if (!transformers.ContainsKey(type))
                    continue;

                results = results.AddRange(await transformers[type](evnt));
            }

            return results;
        }

        public Task<object> PrepareEvent(object evnt)
        {
            var typesToCheck = evnt.GetType().GetInheritedTypes();

            foreach (var type in typesToCheck)
            {
                if (handlers.TryGetValue(type, out var handler))
                    return handler.PrepareEvent(evnt);
            }

            return Task.FromResult(evnt);
        }

        public TIdContext? GetIdContextFor(object evnt)
        {
            var lookupEvent = evnt is IEventEnvelope envelope ? envelope.OriginalEvent : evnt;
            var typesToCheck = lookupEvent.GetType().GetInheritedTypes();

            foreach (var type in typesToCheck)
            {
                if (!handlers.TryGetValue(type, out var handler))
                    continue;

                if (!handler.Filter.FilterEvent(lookupEvent))
                    continue;

                var id = handler.GetId(evnt);
                if (id != null)
                    return id;
            }

            return default;
        }

        public async Task<bool> Handle(
            TContext context,
            object evnt,
            long position,
            CancellationToken cancellationToken)
        {
            // Unwrap envelope for type lookup, but pass the full envelope to the handler
            var lookupEvent = evnt is IEventEnvelope envelope ? envelope.OriginalEvent : evnt;
            var typesToCheck = lookupEvent.GetType().GetInheritedTypes();

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

