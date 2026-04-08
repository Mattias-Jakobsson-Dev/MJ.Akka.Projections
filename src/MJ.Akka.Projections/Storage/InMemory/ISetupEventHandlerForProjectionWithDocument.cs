using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.InMemory;

/// <summary>
/// Returned by <c>WhenExists()</c> on an InMemory handler.
/// Exposes <c>ModifyDocument</c> and <c>DeleteDocument</c> overloads where
/// <typeparamref name="TDocument"/> is non-nullable.
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
    public ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent> When(
        Func<IProjectionFilterSetup<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>,
            IProjectionFilterSetup<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>> filter)
        => inner.When(filter);

    ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>
        ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TEvent>.HandleWith(
            Func<TEvent, InMemoryProjectionContext<TIdContext, TDocument>, long?, CancellationToken, Task> handler)
        => inner.HandleWith(handler);

    public ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TNewEvent> On<TNewEvent>(
        Func<TNewEvent, TIdContext?> getId) => inner.On(getId);

    public ISetupEventHandlerForProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>, TNewEvent> On<TNewEvent>(
        Func<TNewEvent, Task<TIdContext?>> getId) => inner.On(getId);

    public IHandleEventInProjection<TIdContext, InMemoryProjectionContext<TIdContext, TDocument>> Build()
        => inner.Build();
}
