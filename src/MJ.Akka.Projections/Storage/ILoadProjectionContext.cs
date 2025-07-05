namespace MJ.Akka.Projections.Storage;

public interface ILoadProjectionContext<in TId, TContext> where TId : notnull where TContext : IProjectionContext
{
    Task<TContext> Load(TId id, CancellationToken cancellationToken = default);
}