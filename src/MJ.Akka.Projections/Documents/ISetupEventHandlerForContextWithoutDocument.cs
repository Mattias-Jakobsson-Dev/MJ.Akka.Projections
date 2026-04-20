using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Documents;

/// <summary>
/// Generic marker interface for "WhenNotExists" handlers on any context that extends
/// <see cref="ContextWithDocument{TIdContext,TDocument}"/>.
/// Storage-specific <c>ISetupEventHandlerForProjectionWithoutDocument</c> interfaces extend this,
/// allowing <c>CreateDocument</c> extension methods to be defined once in the core library.
/// </summary>
[PublicAPI]
public interface ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, out TContext, out TEvent>
    : ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
    where TIdContext : IProjectionIdContext
    where TDocument : class
    where TContext : ContextWithDocument<TIdContext, TDocument>
{
}

