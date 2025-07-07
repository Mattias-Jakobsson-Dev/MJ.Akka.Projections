using JetBrains.Annotations;
using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public static class ContextWithDocumentExtensions
{
    public static IEnumerable<IProjectionResult> CreateDocument<TId, TDocument>(
        this ContextWithDocument<TId, TDocument> context,
        TDocument document)
        where TId : notnull where TDocument : class
    {
        return context.ModifyDocument(_ => document);
    }
}