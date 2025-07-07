using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public interface IProjectionFilterSetup<TId, TContext, out TEvent> 
    where TId : notnull where TContext : IProjectionContext
{
    IProjectionFilterSetup<TId, TContext, TEvent> WithEventFilter(Func<TEvent, bool> filter);
    IProjectionFilterSetup<TId, TContext, TEvent> WithDocumentFilter(Func<TContext, bool> filter);

    internal IProjectionFilter<TContext> Build();
}