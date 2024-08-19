using System.Diagnostics.CodeAnalysis;

namespace DC.Akka.Projections;

public class DocumentId(object? id, bool hasMatch)
{
    public object? Id { get; } = id;
        
    [MemberNotNullWhen(true, nameof(Id))]
    public bool IsUsable => Id != null && hasMatch;
}