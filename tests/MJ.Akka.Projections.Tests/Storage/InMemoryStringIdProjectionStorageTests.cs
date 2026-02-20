using System.Collections.Immutable;
using FluentAssertions;
using JetBrains.Annotations;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InMemoryStringIdProjectionStorageTests 
    : ProjectionStorageTests<string, InMemoryProjectionContext<string, TestDocument<string>>, SetupInMemoryStorage>
{
    private readonly string _eventId = Guid.NewGuid().ToString();
    
    protected override SetupInMemoryStorage GetStorage()
    {
        return new SetupInMemoryStorage();
    }

    protected override InMemoryProjectionContext<string, TestDocument<string>> CreateInsertRequest(string id)
    {
        return new InMemoryProjectionContext<string, TestDocument<string>>(
            id,
            new TestDocument<string>
            {
                Id = id,
                HandledEvents = ImmutableList.Create(_eventId)
            });
    }

    protected override InMemoryProjectionContext<string, TestDocument<string>> CreateDeleteRequest(string id)
    {
        return new InMemoryProjectionContext<string, TestDocument<string>>(
            id,
            null);
    }
    
    protected override IProjection<string, InMemoryProjectionContext<string, TestDocument<string>>, SetupInMemoryStorage> 
        CreateProjection()
    {
        return new TestProjection<string>(ImmutableList<object>.Empty, ImmutableList<StorageFailures>.Empty);
    }

    protected override Task VerifyContext(InMemoryProjectionContext<string, TestDocument<string>> loaded)
    {
        loaded.Document!.HandledEvents.Should().BeEquivalentTo(ImmutableList.Create(_eventId));

        return Task.CompletedTask;
    }
}