using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Setup;

public static class SetupEventRoutingExtensions
{
    public static ISetupEventRouting<TIdContext, TContext, TEvent> Transform<TIdContext, TContext, TEvent>(
        this ISetupEventRouting<TIdContext, TContext, TEvent> setup,
        Func<TEvent, IImmutableList<object>> transform)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return setup.Transform(evnt => Task.FromResult(transform(evnt)));
    }

    public static ISetupEventRouting<TIdContext, TContext, TEvent, TData>
        Transform<TIdContext, TContext, TEvent, TData>(
            this ISetupEventRouting<TIdContext, TContext, TEvent, TData> setup,
            Func<TEvent, TData, IImmutableList<object>> transform)
        where TIdContext : IProjectionIdContext
        where TContext : IProjectionContext
    {
        return setup.Transform((evnt, data) => Task.FromResult(transform(evnt, data)));
    }
}