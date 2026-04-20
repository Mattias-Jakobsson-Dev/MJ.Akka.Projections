namespace MJ.Akka.Projections.Setup;

/// <summary>
/// Marker interface for events that carry pre-fetched data alongside the original event.
/// </summary>
internal interface IEventEnvelope
{
    object OriginalEvent { get; }
}

/// <summary>
/// Wraps an original event together with data fetched via <c>WithData</c>.
/// This envelope replaces the raw event in the pipeline so the data travels
/// naturally alongside the event all the way through to the handler.
/// </summary>
internal sealed record EventEnvelope<TData>(object OriginalEvent, TData Data) : IEventEnvelope;

