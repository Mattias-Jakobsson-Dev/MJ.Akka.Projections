using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace MJ.Akka.Projections.Storage.RavenDb;

public class SetupRavenDbStorage(IDocumentStore documentStore, BulkInsertOptions insertOptions) : IStorageSetup
{
    public IProjectionStorage CreateProjectionStorage()
    {
        return new RavenDbDocumentProjectionStorage(documentStore, insertOptions);
    }

    public IProjectionPositionStorage CreatePositionStorage()
    {
        return new RavenDbProjectionPositionStorage(documentStore);
    }
    
    internal IDocumentStore GetDocumentStore()
    {
        return documentStore;
    }
}