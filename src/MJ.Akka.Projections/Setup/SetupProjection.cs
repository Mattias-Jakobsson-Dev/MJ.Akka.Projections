using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

internal class SetupProjection<TIdContext, TContext> : ISetupProjection<TIdContext, TContext>
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    private IImmutableDictionary<Type, HandlerBuilder<TIdContext, TContext>> _handlers;

    private readonly IImmutableDictionary<Type, Func<object, IImmutableList<object>>> _transformers;

    public SetupProjection()
        : this(
            ImmutableDictionary<Type, HandlerBuilder<TIdContext, TContext>>.Empty,
            ImmutableDictionary<Type, Func<object, IImmutableList<object>>>.Empty)
    {
    }

    private SetupProjection(
        IImmutableDictionary<Type, HandlerBuilder<TIdContext, TContext>> handlers,
        IImmutableDictionary<Type, Func<object, IImmutableList<object>>> transformers)
    {
        _handlers = handlers;
        _transformers = transformers;
    }

    public ISetupProjection<TIdContext, TContext> TransformUsing<TEvent>(
        Func<TEvent, IImmutableList<object>> transform)
    {
        return new SetupProjection<TIdContext, TContext>(
            _handlers,
            _transformers.SetItem(typeof(TEvent), evnt => transform((TEvent)evnt)));
    }
    
    public ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> On<TEvent>(
        Func<TEvent, Task<TIdContext>> getId,
        Func<
            IProjectionFilterSetup<TIdContext, TContext, TEvent>,
            IProjectionFilterSetup<TIdContext, TContext, TEvent>>? filter = null)
    {
        IProjectionFilterSetup<TIdContext, TContext, TEvent> projectionFilterSetup
            = ProjectionFilterSetup<TIdContext, TContext, TEvent>.Create();

        projectionFilterSetup = filter?.Invoke(projectionFilterSetup) ?? projectionFilterSetup;

        var builder = new HandlerBuilder<TIdContext, TContext, TEvent>(
            getId,
            projectionFilterSetup.Build(),
            this);
        
        _handlers = _handlers.SetItem(typeof(TEvent), builder);
        
        return builder;
    }

    public IHandleEventInProjection<TIdContext, TContext> Build()
    {
        return new EventHandler(
            _handlers.ToImmutableDictionary(
                x => x.Key,
                x => x.Value.Build()),
            _transformers);
    }

    private class EventHandler(
        IImmutableDictionary<Type, HandlerBuilder<TIdContext, TContext>.Handler> handlers,
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

                if (!handler.Filter.FilterResult(context))
                    return handled;

                await handler.Handle(evnt, context, position, cancellationToken);

                handled = true;
            }

            return handled;
        }
    }
}