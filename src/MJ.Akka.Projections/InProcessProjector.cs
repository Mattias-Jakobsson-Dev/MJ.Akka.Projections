using System.Collections.Immutable;

namespace MJ.Akka.Projections;

public class InProcessProjector<TResult>
{
    private readonly IImmutableDictionary<Type, Func<object, TResult, TResult>> _handlers;

    private InProcessProjector(IImmutableDictionary<Type, Func<object, TResult, TResult>> handlers)
    {
        _handlers = handlers;
    }

    public static InProcessProjector<TResult> Setup(Action<IProjectorSetup> runSetup)
    {
        var setup = new ProjectorSetup();

        runSetup(setup);

        return new InProcessProjector<TResult>(setup.Build());
    }
    
    public interface IProjectorSetup
    {
        IProjectorSetup On<TEvent>(Func<TEvent, TResult, TResult> handler);
    }
    
    private class ProjectorSetup : IProjectorSetup
    {
        private readonly Dictionary<Type, Func<object, TResult, TResult>> _handlers = new();
        
        public IProjectorSetup On<TEvent>(Func<TEvent, TResult, TResult> handler)
        {
            _handlers[typeof(TEvent)] = (evnt, result) => handler((TEvent)evnt, result);

            return this;
        }

        public IImmutableDictionary<Type, Func<object, TResult, TResult>> Build()
        {
            return _handlers.ToImmutableDictionary();
        }
    }
    
    public (IImmutableList<object> unhandledEvents, TResult result) RunFor(
        IImmutableList<object> events,
        TResult initialData)
    {
        var result = initialData;
        var unhandledEvents = ImmutableList.CreateBuilder<object>();

        foreach (var evnt in events)
        {
            var typesToCheck = evnt.GetType().GetInheritedTypes();

            var handled = false;

            foreach (var type in typesToCheck)
            {
                if (!_handlers.TryGetValue(type, out var handler))
                    continue;

                result = handler(evnt, result);

                handled = true;
            }
            
            if (!handled)
                unhandledEvents.Add(evnt);
        }
        
        return (unhandledEvents.ToImmutable(), result);
    }
}