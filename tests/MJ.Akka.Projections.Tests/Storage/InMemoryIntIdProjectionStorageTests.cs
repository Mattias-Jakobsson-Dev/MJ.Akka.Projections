using System.Collections.Immutable;
using FluentAssertions;
using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Storage.Messages;
using MJ.Akka.Projections.Tests.TestData;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InMemoryIntIdProjectionStorageTests 
    : ProjectionStorageTests<int, InMemoryProjectionContext<int, TestDocument<int>>, SetupInMemoryStorage>
{
    private readonly string _eventId = Guid.NewGuid().ToString();
    
    protected override SetupInMemoryStorage GetStorage()
    {
        return new SetupInMemoryStorage();
    }

    protected override StoreProjectionRequest CreateInsertRequest(int id)
    {
        return new StoreProjectionRequest(ImmutableList.Create<IProjectionResult>(
            new DocumentResults.DocumentCreated(id, new TestDocument<int>
            {
                Id = id,
                HandledEvents = ImmutableList.Create(_eventId)
            })));
    }

    protected override StoreProjectionRequest CreateDeleteRequest(int id)
    {
        return new StoreProjectionRequest(ImmutableList.Create<IProjectionResult>(
            new DocumentResults.DocumentDeleted(id)));
    }

    protected override IProjection<int, InMemoryProjectionContext<int, TestDocument<int>>, SetupInMemoryStorage> 
        CreateProjection()
    {
        return new TestProjection<int>(ImmutableList<object>.Empty, ImmutableList<StorageFailures>.Empty);
    }

    protected override Task VerifyContext(InMemoryProjectionContext<int, TestDocument<int>> loaded)
    {
        loaded.Document!.HandledEvents.Should().BeEquivalentTo(ImmutableList.Create(_eventId));

        return Task.CompletedTask;
    }
}