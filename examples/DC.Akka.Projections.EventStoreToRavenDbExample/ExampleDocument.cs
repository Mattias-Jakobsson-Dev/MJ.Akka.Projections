using System.Collections.Immutable;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.EventStoreToRavenDbExample;

[PublicAPI]
public class ExampleDocument
{
    public required string Id { get; set; }
    public required string Slug { get; set; }
    public IImmutableDictionary<string, object> ProjectedEvents { get; set; } 
        = ImmutableDictionary<string, object>.Empty;

    public static string BuildId(string slug)
    {
        return $"examples/{slug}";
    }
}