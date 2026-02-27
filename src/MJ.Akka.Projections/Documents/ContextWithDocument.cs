using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public abstract class ContextWithDocument<TIdContext, TDocument>(TIdContext id, TDocument? document) : IProjectionContext
    where TIdContext : IProjectionIdContext where TDocument : class
{
    public TIdContext Id { get; } = id;
    public TDocument? Document { get; protected set; } = document;
    
    [MemberNotNullWhen(true, nameof(Document))]
    public bool Exists()
    {
        return Document != null;
    }

    public virtual IProjectionContext MergeWith(IProjectionContext later)
    {
        if (later is ContextWithDocument<TIdContext, TDocument> parsedLater)
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
