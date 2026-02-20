using JetBrains.Annotations;

namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public static class ContextWithDocumentExtensions
{
    public static void CreateDocument<TId, TDocument>(
        this ContextWithDocument<TId, TDocument> context,
        TDocument document)
        where TId : notnull where TDocument : class
    {
        context.ModifyDocument(_ => document);
    }
}