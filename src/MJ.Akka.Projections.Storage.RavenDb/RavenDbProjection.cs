using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public abstract class RavenDbProjection<TDocument> 
    : BaseProjection<string, RavenDbProjectionContext<TDocument>, SetupRavenDbStorage>
    where TDocument : class
{
    public override string IdFromString(string id)
    {
        return id;
    }

    public override string IdToString(string id)
    {
        return id;
    }
    
    public override ILoadProjectionContext<string, RavenDbProjectionContext<TDocument>> GetLoadProjectionContext(
        SetupRavenDbStorage storageSetup)
    {
        return new LoadProjectionDataFromRavenDb<TDocument>(storageSetup.GetDocumentStore());
    }
}