using JetBrains.Annotations;
using Raven.Client.Documents;
using Raven.Embedded;
using Raven.TestDriver;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbFixture : RavenTestDriver
{
    static RavenDbFixture()
    {
        ConfigureServer(new TestServerOptions
        {
            Licensing = new ServerOptions.LicensingOptions
            {
                EulaAccepted = true,
                ThrowOnInvalidOrMissingLicense = false
            }
        });
    }
    
    public IDocumentStore OpenDocumentStore()
    {
        return GetDocumentStore();
    }
}