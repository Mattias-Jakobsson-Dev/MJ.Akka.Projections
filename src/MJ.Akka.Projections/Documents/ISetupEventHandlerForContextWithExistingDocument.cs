using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Documents;

/// <summary>
/// Generic marker interface for "WhenExists" handlers on any context that extends
/// <see cref="ContextWithDocument{TIdContext,TDocument}"/>.
/// Storage-specific <c>ISetupEventHandlerForProjectionWithExistingDocument</c> interfaces extend this,
/// allowing non-nullable <c>ModifyDocument</c> extension methods to be defined once in the core library.
/// </summary>
[PublicAPI]
public interface ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, out TContext, out TEvent>
    : ISetupEventHandlerForProjection<TIdContext, TContext, TEvent>
    where TIdContext : IProjectionIdContext
    where TDocument : class
    where TContext : ContextWithDocument<TIdContext, TDocument>;

/// <summary>
/// Generic marker interface for "WhenExists" handlers on any context that extends
/// <see cref="ContextWithDocument{TIdContext,TDocument}"/> with additional fetched data of type <typeparamref name="TData"/>.
/// </summary>
[PublicAPI]
public interface ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, out TContext, out TEvent, out TData>
    : ISetupEventHandlerForProjection<TIdContext, TContext, TEvent, TData>
    where TIdContext : IProjectionIdContext
    where TDocument : class
    where TContext : ContextWithDocument<TIdContext, TDocument>;

