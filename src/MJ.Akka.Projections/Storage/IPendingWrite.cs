using System.Collections.Immutable;
using Akka;

namespace MJ.Akka.Projections.Storage;

public interface IPendingWrite
{
    CancellationToken CancellationToken { get; }
    IImmutableList<DocumentToStore> ToUpsert { get; }
    IImmutableList<DocumentToDelete> ToDelete { get; }

    IPendingWrite MergeWith(IPendingWrite other);

    void Completed();

    void Fail(Exception exception);

    internal IImmutableList<TaskCompletionSource<NotUsed>> GetCompletions();
}