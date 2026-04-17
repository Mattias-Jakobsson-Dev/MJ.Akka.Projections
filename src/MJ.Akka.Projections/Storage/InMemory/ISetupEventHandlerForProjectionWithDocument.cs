using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.InMemory;

/// <summary>
/// Returned by <c>WhenExists()</c> on an InMemory handler.
/// Exposes <c>ModifyDocument</c> overloads where <typeparamref name="TDocument"/> is non-nullable.
/// </summary>
[PublicAPI]
public interface ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent>
    : ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
    where TIdContext : IProjectionIdContext
    where TDocument : class
{
}

/// <summary>
/// Returned by <c>WhenNotExists()</c> on an InMemory handler.
/// Exposes <c>CreateDocument</c> overloads.
/// </summary>
[PublicAPI]
public interface ISetupEventHandlerForProjectionWithoutDocument<TIdContext, TDocument, TEvent>
    : ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
    where TIdContext : IProjectionIdContext
    where TDocument : class
{
}

internal sealed class SetupEventHandlerForProjectionWithDocument<TIdContext, TDocument, TEvent>(
    ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> inner)
    : ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent>,
      ISetupEventHandlerForProjectionWithoutDocument<TIdContext, TDocument, TEvent>
    where TIdContext : IProjectionIdContext
    where TDocument : class
{
    public ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> HandleWith(
        Func<TEvent, InMemoryProjectionContext<TIdContext, TDocument>, long?, CancellationToken, Task> handler)
        => inner.HandleWith(handler);
}
