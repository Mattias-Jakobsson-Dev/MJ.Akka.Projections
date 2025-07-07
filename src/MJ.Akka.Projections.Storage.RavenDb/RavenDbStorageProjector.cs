using MJ.Akka.Projections.Documents;

namespace MJ.Akka.Projections.Storage.RavenDb;

public static class RavenDbStorageProjector
{
    public static InProcessProjector<RavenDbStorageProjectorResult> Setup()
    {
        return DocumentsStorageProjector
            .Setup<RavenDbStorageProjectorResult>(setup => setup
                .On<DocumentResults.DocumentDeleted>((evnt, result) =>
                    result with
                    {
                        DocumentsToUpsert = result.DocumentsToUpsert.Remove(evnt.Id),
                        TimeSeriesToAdd = result.TimeSeriesToAdd.Remove((string)evnt.Id),
                        DocumentsToDelete = result.DocumentsToDelete.Add(evnt.Id)
                    })
                .On<StoreTimeSeries>((evnt, result) => result.WithTimeSeries(
                    evnt.DocumentId,
                    evnt.Name,
                    evnt.Records)));
    }
}