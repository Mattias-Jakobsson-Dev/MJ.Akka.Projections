using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Storage;

public interface ILoadProjectionContext<TIdContext, TContext> 
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext
{
    Task<TContext> Load(
        TIdContext id,
        Func<TIdContext, TContext> getDefaultContext,
        CancellationToken cancellationToken = default);
}