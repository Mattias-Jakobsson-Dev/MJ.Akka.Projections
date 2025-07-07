using System.Collections.Immutable;
using InfluxDB.Client.Writes;

namespace MJ.Akka.Projections.Storage.InfluxDb;

public static class InfluxDbStorageProjector
{
    public static InProcessProjector<InfluxDbStorageProjectorResult> Setup()
    {
        return InProcessProjector<InfluxDbStorageProjectorResult>.Setup(setup => setup
            .On<InfluxDbWritePoint>((evnt, result) =>
                result with
                {
                    PointsToWrite = result
                        .PointsToWrite
                        .SetItem(evnt.Id, 
                            result.PointsToWrite.TryGetValue(evnt.Id, out var existingPoints)
                                ? existingPoints.Add(evnt.Data)
                                : ImmutableList<PointData>.Empty.Add(evnt.Data))
                })
            .On<InfluxDbDeletePoint>((evnt, result) =>
                result with
                {
                    PointsToDelete = result.PointsToDelete.Add(evnt)
                }));
    }
}