using System.Collections.Immutable;
using System.Diagnostics;

namespace DC.Akka.Projections.InProc;

public class MaxNumberOfProjectorsPassivation(int maxNumberOfProjectors) : IHandleProjectionPassivation
{
    public static MaxNumberOfProjectorsPassivation Default { get; } = new(1_000);
    
    public IHandleProjectionPassivation.IHandler StartNew()
    {
        return new Handler(maxNumberOfProjectors);
    }
    
    private class Handler(int maxNumberOfProjectors) : IHandleProjectionPassivation.IHandler
    {
        private readonly Dictionary<string, Stopwatch> _runningProjectors = new();
        
        public void SetAndMaybeRemove(string id, Action<string> remove)
        {
            _runningProjectors[id] = Stopwatch.StartNew();
            
            var numberOfItemsToRemove = _runningProjectors.Count - maxNumberOfProjectors;

            if (numberOfItemsToRemove <= 0)
                return;
            
            var itemsToRemove = _runningProjectors
                .OrderByDescending(x => x.Value.Elapsed)
                .Select(x => x.Key)
                .Take(numberOfItemsToRemove)
                .ToImmutableList();

            foreach (var itemToRemove in itemsToRemove)
                remove(itemToRemove);
        }
    }
}