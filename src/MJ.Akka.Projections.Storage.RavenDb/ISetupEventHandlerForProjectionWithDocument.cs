using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.RavenDb;

/// <summary>
/// Returned by <c>WhenExists()</c> on a RavenDb handler.
/// Exposes <c>ModifyDocument</c> and <c>DeleteDocument</c> overloads where
/// <typeparamref name="TDocument"/> is non-nullable.
/// </summary>
[PublicAPI]
public interface ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent>
    : ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
    where TIdContext : IProjectionIdContext
    where TDocument : class
{
}

/// <summary>
/// Returned by <c>WhenNotExists()</c> on a RavenDb handler.
/// Exposes <c>CreateDocument</c> overloads.
/// </summary>
[PublicAPI]
public interface ISetupEventHandlerForProjectionWithoutDocument<TIdContext, TDocument, TEvent>
    : ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
    where TIdContext : IProjectionIdContext
    where TDocument : class
{
}

internal sealed class SetupEventHandlerForProjectionWithDocument<TIdContext, TDocument, TEvent>(
    ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> inner)
    : ISetupEventHandlerForProjectionWithExistingDocument<TIdContext, TDocument, TEvent>,
      ISetupEventHandlerForProjectionWithoutDocument<TIdContext, TDocument, TEvent>
    where TIdContext : IProjectionIdContext
    where TDocument : class
{
    public ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent> When(
        Func<IProjectionFilterSetup<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>,
            IProjectionFilterSetup<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>> filter)
        => inner.When(filter);

    ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>
        ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TEvent>.HandleWith(
            Func<TEvent, RavenDbProjectionContext<TDocument, TIdContext>, long?, CancellationToken, Task> handler)
        => inner.HandleWith(handler);

    public ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TNewEvent> On<TNewEvent>(
        Func<TNewEvent, TIdContext?> getId) => inner.On(getId);

    public ISetupEventHandlerForProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>, TNewEvent> On<TNewEvent>(
        Func<TNewEvent, Task<TIdContext?>> getId) => inner.On(getId);

    public IHandleEventInProjection<TIdContext, RavenDbProjectionContext<TDocument, TIdContext>> Build()
        => inner.Build();
}
