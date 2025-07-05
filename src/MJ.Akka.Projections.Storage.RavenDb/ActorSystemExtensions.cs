using Akka.Actor;
using MJ.Akka.Projections.Configuration;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;

namespace MJ.Akka.Projections.Storage.RavenDb;

public static class ActorSystemExtensions
{
    public static IConfigureProjectionCoordinator RavenDbProjections(
        this ActorSystem actorSystem,
        Func<IHaveConfiguration<ProjectionSystemConfiguration<SetupRavenDbStorage>>,
                IHaveConfiguration<ProjectionSystemConfiguration<SetupRavenDbStorage>>>
            configure,
        IDocumentStore documentStore,
        BulkInsertOptions? insertOptions = null)
    {
        return actorSystem.Projections(
            configure,
            new SetupRavenDbStorage(documentStore, insertOptions ?? new BulkInsertOptions()));
    }
}