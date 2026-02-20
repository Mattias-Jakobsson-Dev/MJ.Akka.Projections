using System.Collections.Immutable;
using InfluxDB.Client.Writes;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.InfluxDb;

[PublicAPI]
public class InfluxDbTimeSeriesContext : IProjectionContext
{
    public InfluxDbTimeSeriesContext(InfluxDbTimeSeriesId id) : this(id, ImmutableList<IInfluxDbOperation>.Empty)
    {
        
    }

    private InfluxDbTimeSeriesContext(InfluxDbTimeSeriesId id, IImmutableList<IInfluxDbOperation> operations)
    {
        Id = id;
        Operations = operations;
    }
    
    public InfluxDbTimeSeriesId Id { get; }
    internal IImmutableList<IInfluxDbOperation> Operations { get; private set; }

    public void AddPoints(IImmutableList<PointData> points)
    {
        Operations = Operations.Add(new InfluxDbWritePoint(Id, points));
    }

    public void DeletePoint(DeleteTimeSeriesInput input)
    {
        Operations = Operations.Add(new InfluxDbDeletePoint(Id, input.Start, input.Stop, input.Predicate));
    }
    
    public bool Exists()
    {
        return true;
    }

    public IProjectionContext MergeWith(IProjectionContext later)
    {
        if (later is InfluxDbTimeSeriesContext parsedLater)
        {
            return new InfluxDbTimeSeriesContext(
                Id,
                Operations.AddRange(parsedLater.Operations));
        }

        return later;
    }

    public IProjectionContext Freeze()
    {
        var currentOperations = Operations;
        
        Operations = ImmutableList<IInfluxDbOperation>.Empty;
        
        return new InfluxDbTimeSeriesContext(Id, currentOperations);
    }
}