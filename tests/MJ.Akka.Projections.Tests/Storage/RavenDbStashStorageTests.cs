using System.Collections.Immutable;
using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.RavenDb;
using Shouldly;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class RavenDbStashStorageTests(RavenDbFixture fixture) : StashStorageTests, IClassFixture<RavenDbFixture>
{
    protected override IProjectionStashStorage GetStorage() =>
        new RavenDbProjectionStashStorage(fixture.OpenDocumentStore());

    protected override IProjectionStashStorage GetStorageWithTimeout(TimeSpan timeout) =>
        new RavenDbProjectionStashStorage(fixture.OpenDocumentStore(), inProcessTimeout: timeout);

    [Fact]
    public async Task AckUnstash_all_events_deletes_the_ravendb_document()
    {
        var store = fixture.OpenDocumentStore();
        var storage = new RavenDbProjectionStashStorage(store);

        var id = new ProjectionContextId(
            Guid.NewGuid().ToString(),
            new SimpleIdContext<string>(Guid.NewGuid().ToString()));

        var events = new[] { 1, 2, 3 }
            .Select(p => new EventWithPosition(new object(), p))
            .ToImmutableList();

        await storage.Stash(id, events);
        var (_, token) = await storage.Unstash(id);
        await storage.AckUnstash(token);

        using var session = store.OpenAsyncSession();
        var doc = await session.LoadAsync<ProjectionStash>(ProjectionStash.BuildId(id));
        doc.ShouldBeNull();
    }
}

