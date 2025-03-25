using JetBrains.Annotations;
using Raven.Client.Documents;
using Raven.TestDriver;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbFixture : RavenTestDriver
{
    public IDocumentStore OpenDocumentStore()
    {
        return GetDocumentStore();
    }
}