using System.Collections.Immutable;

namespace MJ.Akka.Projections.Documents;

public static class DocumentsStorageProjector
{
    public static InProcessProjector<TResult> Setup<TResult>(
        Action<InProcessProjector<TResult>.IProjectorSetup> additionalSetup)
        where TResult : DocumentsStorageProjectorResult
    {
        return InProcessProjector<TResult>.Setup(setup =>
        {
            setup
                .On<DocumentResults.DocumentCreated>((evnt, result) =>
                    result with
                    {
                        DocumentsToUpsert = result.DocumentsToUpsert.SetItem(evnt.Id, evnt.Document),
                        DocumentsToDelete = result.DocumentsToDelete.Remove(evnt.Id)
                    })
                .On<DocumentResults.DocumentModified>((evnt, result) =>
                    result with
                    {
                        DocumentsToUpsert = result.DocumentsToUpsert.SetItem(evnt.Id, evnt.Document),
                        DocumentsToDelete = result.DocumentsToDelete.Remove(evnt.Id)
                    })
                .On<DocumentResults.DocumentDeleted>((evnt, result) =>
                    result with
                    {
                        DocumentsToUpsert = result.DocumentsToUpsert.Remove(evnt.Id),
                        DocumentsToDelete = result.DocumentsToDelete.Add(evnt.Id)
                    });

            additionalSetup(setup);
        });
    }
}

public abstract record DocumentsStorageProjectorResult(
    IImmutableDictionary<object, object> DocumentsToUpsert,
    IImmutableList<object> DocumentsToDelete);