using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public record TimeSeriesInput(string Name, DateTime Timestamp, IImmutableList<double> Values, string? Tag = null)
{
    public TimeSeriesInput(string name, DateTime timeStamp, double value, string? tag = null)
        : this(name, timeStamp, ImmutableList.Create(value), tag)
    {
        
    }
}