using System.Collections.Immutable;

namespace MJ.Akka.Projections.Storage;

public interface IProjectionStorage
{
    Task Store(
        IImmutableDictionary<ProjectionContextId, IProjectionContext> contexts,
        CancellationToken cancellationToken = default);
}