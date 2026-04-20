using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Storage.RavenDb;

/// <summary>
/// Returned by <c>WhenExists()</c> on a RavenDb handler.
/// Exposes <c>ModifyDocument</c> overloads where <typeparamref name="TDocument"/> is non-nullable.
/// </summary>
[PublicAPI]
public interface ISetupEventHandlerForProjectionWithExistingDocument<TDocument, TEvent>
    : ISetupEventHandlerForProjection<SimpleIdContext<string>, RavenDbProjectionContext<TDocument>, TEvent>,
      ISetupEventHandlerForContextWithExistingDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent>
    where TDocument : class
{
}

/// <summary>
/// Returned by <c>WhenNotExists()</c> on a RavenDb handler.
/// Exposes <c>CreateDocument</c> overloads.
/// </summary>
[PublicAPI]
public interface ISetupEventHandlerForProjectionWithoutDocument<TDocument, TEvent>
    : ISetupEventHandlerForContextWithoutDocument<SimpleIdContext<string>, TDocument, RavenDbProjectionContext<TDocument>, TEvent>
    where TDocument : class
{
}


