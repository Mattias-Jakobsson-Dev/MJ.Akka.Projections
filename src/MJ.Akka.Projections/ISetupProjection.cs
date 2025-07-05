using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public interface ISetupProjection<TId, TContext> where TId : notnull where TContext : IProjectionContext<TId>
{
    ISetupProjection<TId, TContext> TransformUsing<TEvent>(
        Func<TEvent, IImmutableList<object>> transform);

    ISetupProjection<TId, TContext> On<TEvent>(
        Func<TEvent, TId> getId,
        Action<TEvent, TContext> handler,
        Func<IProjectionFilterSetup<TId, TContext, TEvent>, IProjectionFilterSetup<TId, TContext, TEvent>>? filter = null);

    ISetupProjection<TId, TContext> On<TEvent>(
        Func<TEvent, TId> getId,
        Action<TEvent, TContext, long> handler,
        Func<IProjectionFilterSetup<TId, TContext, TEvent>, IProjectionFilterSetup<TId, TContext, TEvent>>? filter = null);

    ISetupProjection<TId, TContext> On<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TContext, Task> handler,
        Func<IProjectionFilterSetup<TId, TContext, TEvent>, IProjectionFilterSetup<TId, TContext, TEvent>>? filter = null);

    ISetupProjection<TId, TContext> On<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TContext, long, Task> handler,
        Func<IProjectionFilterSetup<TId, TContext, TEvent>, IProjectionFilterSetup<TId, TContext, TEvent>>? filter = null);

    ISetupProjection<TId, TContext> On<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TContext, CancellationToken, Task> handler,
        Func<IProjectionFilterSetup<TId, TContext, TEvent>, IProjectionFilterSetup<TId, TContext, TEvent>>? filter = null);

    ISetupProjection<TId, TContext> On<TEvent>(
        Func<TEvent, TId> getId,
        Func<TEvent, TContext, long, CancellationToken, Task> handler,
        Func<IProjectionFilterSetup<TId, TContext, TEvent>, IProjectionFilterSetup<TId, TContext, TEvent>>? filter = null);

    IHandleEventInProjection<TId, TContext> Build();
}