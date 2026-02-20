using System.Collections.Concurrent;
using System.Collections.Immutable;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.InMemory;

namespace MJ.Akka.Projections.TestKit;

public class ProjectionTestStorageSetup(ConcurrentDictionary<ProjectionContextId, IProjectionContext> storage) 
    : IStorageSetup
{
    internal ConcurrentDictionary<ProjectionContextId, IProjectionContext> Storage { get; } = storage;
    
    public IProjectionStorage CreateProjectionStorage()
    {
        return new ProjectionTestStorage(Storage);
    }

    public IProjectionPositionStorage CreatePositionStorage()
    {
        return new InMemoryPositionStorage();
    }
    
    private class ProjectionTestStorage(ConcurrentDictionary<ProjectionContextId, IProjectionContext> storage) 
        : IProjectionStorage
    {
        public Task Store(
            IImmutableDictionary<ProjectionContextId, IProjectionContext> contexts, 
            CancellationToken cancellationToken = default)
        {
            foreach (var context in contexts)
            {
                storage
                    .AddOrUpdate(
                        context.Key,
                        _ => context.Value,
                        (_, current) => current.MergeWith(context.Value).Freeze());
            }
            
            return Task.CompletedTask;
        }
    }
}