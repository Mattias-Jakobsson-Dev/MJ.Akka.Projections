using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.InMemory;

/// <summary>
/// Returned by <c>WhenExists()</c> on an InMemory handler.
/// Exposes <c>ModifyDocument</c> overloads where <typeparamref name="TDocument"/> is non-nullable.
/// </summary>
[PublicAPI]
public interface ISetupEventHandlerForProjectionWithExistingDocument<TId, TDocument, TEvent>
    : ISetupEventHandlerForProjection<SimpleIdContext<TId>, InMemoryProjectionContext<TId, TDocument>, TEvent>,
      ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<TId>, TDocument, InMemoryProjectionContext<TId, TDocument>, TEvent>
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
    : ISetupEventHandlerForContextWithoutDocument<SimpleIdContext<TId>, TDocument, InMemoryProjectionContext<TId, TDocument>, TEvent>
    where TId : notnull
    where TDocument : class
{
}


