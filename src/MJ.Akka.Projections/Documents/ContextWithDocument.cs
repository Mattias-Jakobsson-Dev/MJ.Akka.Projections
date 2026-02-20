using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public abstract class ContextWithDocument<TId, TDocument>(TId id, TDocument? document) : IProjectionContext
    where TId : notnull where TDocument : class
{
    public TId Id { get; } = id;
    public TDocument? Document { get; protected set; } = document;
    
    [MemberNotNullWhen(true, nameof(Document))]
    public bool Exists()
    {
        return Document != null;
    }

    public virtual IProjectionContext MergeWith(IProjectionContext later)
    {
        if (later is ContextWithDocument<TId, TDocument> parsedLater)
            Document = parsedLater.Document;

        return later.Freeze();
    }

    public abstract IProjectionContext Freeze();

    public void ModifyDocument(Func<TDocument?, TDocument> modification)
    {
        Document = modification(Document);
    }
    
    public async Task ModifyDocument(Func<TDocument?, Task<TDocument>> modification)
    {
        Document = await modification(Document);
    }

    public void DeleteDocument()
    {
        Document = null;
    }
}
