using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public abstract class ContextWithDocument<TId, TDocument>(TId id, TDocument? document) : IProjectionContext
    where TId : notnull where TDocument : class
{
    public TId Id { get; } = id;
    public TDocument? Document { get; private set; } = document;
    
    [MemberNotNullWhen(true, nameof(Document))]
    public bool Exists()
    {
        return Document != null;
    }

    public IEnumerable<IProjectionResult> ModifyDocument(Func<TDocument?, TDocument> modification)
    {
        Document = modification(Document);

        return [Exists()
            ? new DocumentResults.DocumentModified(Id, Document) 
            : new DocumentResults.DocumentCreated(Id, Document)];
    }
    
    public async Task<IEnumerable<IProjectionResult>> ModifyDocument(Func<TDocument?, Task<TDocument>> modification)
    {
        Document = await modification(Document);

        return [Exists()
            ? new DocumentResults.DocumentModified(Id, Document) 
            : new DocumentResults.DocumentCreated(Id, Document)];
    }

    public IEnumerable<IProjectionResult> DeleteDocument()
    {
        if (!Exists())
            return [];

        return [new DocumentResults.DocumentDeleted(Id)];
    }
}
