using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections;

public interface IProjectionContext
{
    bool Exists();

    PrepareForStorageResponse PrepareForStorage();
}

public record PrepareForStorageResponse(object Id, IImmutableList<ICanBePersisted> Items, IProjectionContext Context);

public interface IProjectionContext<out TId> : IProjectionContext where TId : notnull
{
    TId Id { get; }
}

public abstract class ProjectedDocumentContext<TId, TDocument>(TId id, TDocument? document) : IProjectionContext<TId> 
    where TId : notnull
{
    public TId Id { get; } = id;
    public TDocument? Document { get; private set; } = document;
    
    [MemberNotNullWhen(true, nameof(Document))]
    public bool Exists()
    {
        return Document != null;
    }

    public abstract PrepareForStorageResponse PrepareForStorage();

    public void ModifyDocument(Func<TDocument?, TDocument?> modifier)
    {
        Document = modifier(Document);
    }
}
