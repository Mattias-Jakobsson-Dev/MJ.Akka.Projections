using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public static class ContextWithDocumentExtensions
{
    public static void CreateDocument<TIdContext, TDocument>(
        this ContextWithDocument<TIdContext, TDocument> context,
        TDocument document)
        where TIdContext : IProjectionIdContext where TDocument : class
    {
        context.ModifyDocument(_ => document);
    }
}