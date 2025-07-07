using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public static class ProjectionFilterSetupExtensions
{
    public static IProjectionFilterSetup<TId, TContext, TEvent> RequireExistingDocument<TId, TContext, TEvent>(
        this IProjectionFilterSetup<TId, TContext, TEvent> filterSetup)
        where TId : notnull where TContext : IProjectionContext
    {
        return filterSetup
            .WithDocumentFilter(doc => doc.Exists());
    }
    
    public static IProjectionFilterSetup<TId, TContext, TEvent> RequireNonExistingDocument<TId, TContext, TEvent>(
        this IProjectionFilterSetup<TId, TContext, TEvent> filterSetup)
        where TId : notnull where TContext : IProjectionContext
    {
        return filterSetup
            .WithDocumentFilter(doc => !doc.Exists());
    }
}