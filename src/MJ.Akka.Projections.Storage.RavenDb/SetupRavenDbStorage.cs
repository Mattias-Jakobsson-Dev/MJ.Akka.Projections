using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace MJ.Akka.Projections.Storage.RavenDb;

public class SetupRavenDbStorage(
    IDocumentStore documentStore,
    BulkInsertOptions insertOptions,
    TimeSpan? stashInProcessTimeout = null) : IStorageSetup
{
    public IProjectionStorage CreateProjectionStorage()
    {
        return new RavenDbDocumentProjectionStorage(documentStore, insertOptions);
    }

    public IProjectionPositionStorage CreatePositionStorage()
    {
        return new RavenDbProjectionPositionStorage(documentStore);
    }

    public IProjectionStashStorage CreateStashStorage()
    {
        return new RavenDbProjectionStashStorage(documentStore, stashInProcessTimeout);
    }
    
    internal IDocumentStore GetDocumentStore()
    {
        return documentStore;
    }
}