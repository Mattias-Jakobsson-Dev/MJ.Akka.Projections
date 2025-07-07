using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public class RavenDbProjectionContext<TDocument>(
    string id,
    TDocument? document)
    : ContextWithDocument<string, TDocument>(id, document), IResettableProjectionContext
    where TDocument : class
{
    public IProjectionContext Reset()
    {
        return new RavenDbProjectionContext<TDocument>(Id, Document);
    }
}