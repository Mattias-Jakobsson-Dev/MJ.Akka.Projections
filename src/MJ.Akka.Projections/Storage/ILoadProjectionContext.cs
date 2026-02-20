namespace MJ.Akka.Projections.Storage;

public interface ILoadProjectionContext<TId, TContext> where TId : notnull where TContext : IProjectionContext
{
    Task<TContext> Load(TId id, Func<TId, TContext> getDefaultContext, CancellationToken cancellationToken = default);
}