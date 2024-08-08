using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Storage.RavenDb;
using DC.Akka.Projections.Tests.TestData;
using JetBrains.Annotations;
using Xunit;

namespace DC.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbProjectionStorageTests(RavenDbFixture fixture) 
    : ProjectionStorageTests<string>, IClassFixture<RavenDbFixture>
{
    protected override string CreateRandomId()
    {
        return Guid.NewGuid().ToString();
    }

    protected override IProjectionStorage<string, TestDocument<string>> GetStorage()
    {
        return new RavenDbProjectionStorage<TestDocument<string>>(fixture.OpenDocumentStore());
    }
}