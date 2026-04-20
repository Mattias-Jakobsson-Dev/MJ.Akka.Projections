using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Setup;

namespace MJ.Akka.Projections.Documents;

internal sealed class SetupEventHandlerForContextWithDocument<TIdContext, TDocument, TContext, TEvent>(
    ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> inner)
    : ISetupEventHandlerForContextWithExistingDocument<TIdContext, TDocument, TContext, TEvent>,
      ISetupEventHandlerForContextWithoutDocument<TIdContext, TDocument, TContext, TEvent>
    where TIdContext : IProjectionIdContext
    where TDocument : class
    where TContext : ContextWithDocument<TIdContext, TDocument>
{
    public ISetupEventHandlerForProjection<TIdContext, TContext, TEvent> HandleWith(
        Func<TEvent, TContext, long?, CancellationToken, Task> handler)
        => inner.HandleWith(handler);
}

