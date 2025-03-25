using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public static class ProjectionFilterSetupExtensions
{
    public static IProjectionFilterSetup<TDocument, TEvent> RequireExistingDocument<TDocument, TEvent>(
        this IProjectionFilterSetup<TDocument, TEvent> filterSetup)
        where TDocument : notnull
    {
        return filterSetup
            .WithDocumentFilter(doc => doc != null);
    }
}