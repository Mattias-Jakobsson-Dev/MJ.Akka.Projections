using System.Collections.Immutable;
using FluentAssertions;
using JetBrains.Annotations;
using MJ.Akka.Projections.Storage.InMemory;
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

    protected override InMemoryProjectionContext<int, TestDocument<int>> CreateInsertRequest(int id)
    {
        return new InMemoryProjectionContext<int, TestDocument<int>>(
            id,
            new TestDocument<int>
            {
                Id = id,
                HandledEvents = ImmutableList.Create(_eventId)
            });
    }

    protected override InMemoryProjectionContext<int, TestDocument<int>> CreateDeleteRequest(int id)
    {
        return new InMemoryProjectionContext<int, TestDocument<int>>(
            id,
            null);
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