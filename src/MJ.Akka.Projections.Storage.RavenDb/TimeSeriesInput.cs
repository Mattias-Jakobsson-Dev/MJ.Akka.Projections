using System.Collections.Immutable;

namespace MJ.Akka.Projections.Storage.RavenDb;

public record TimeSeriesInput(string Name, DateTime Timestamp, IImmutableList<double> Values, string? Tag = null)
{
    public TimeSeriesInput(string name, DateTime timeStamp, double value, string? tag = null)
        : this(name, timeStamp, ImmutableList.Create(value), tag)
    {
        
    }
}