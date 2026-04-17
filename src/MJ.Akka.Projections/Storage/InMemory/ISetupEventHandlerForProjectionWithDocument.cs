using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.InMemory;

/// <summary>
/// Returned by <c>WhenExists()</c> on an InMemory handler.
/// Exposes <c>ModifyDocument</c> overloads where <typeparamref name="TDocument"/> is non-nullable.
/// </summary>
[PublicAPI]
public interface ISetupEventHandlerForProjectionWithExistingDocument<TId, TDocument, TEvent>
    : ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
    where TId : notnull
    where TDocument : class
{
}

/// <summary>
/// Returned by <c>WhenNotExists()</c> on an InMemory handler.
/// Exposes <c>CreateDocument</c> overloads.
/// </summary>
[PublicAPI]
public interface ISetupEventHandlerForProjectionWithoutDocument<TId, TDocument, TEvent>
    : ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>
    where TId : notnull
    where TDocument : class
{
}

internal sealed class SetupEventHandlerForProjectionWithDocument<TId, TDocument, TEvent>(
    ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> inner)
    : ISetupEventHandlerForProjectionWithExistingDocument<TId, TDocument, TEvent>,
      ISetupEventHandlerForProjectionWithoutDocument<TId, TDocument, TEvent>
    where TId : notnull
    where TDocument : class
{
    public ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent> HandleWith(
        Func<TEvent, InMemoryProjectionContext<TId, TDocument>, long?, CancellationToken, Task> handler)
        => inner.HandleWith(handler);
}
