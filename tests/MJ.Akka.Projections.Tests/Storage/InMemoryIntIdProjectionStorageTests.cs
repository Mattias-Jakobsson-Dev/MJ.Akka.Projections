using System.Collections.Immutable;
using Shouldly;
using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InMemoryIntIdProjectionStorageTests 
    : ProjectionStorageTests<SimpleIdContext<int>, InMemoryProjectionContext<int, TestDocument<int>>, SetupInMemoryStorage>
{
    private readonly string _eventId = Guid.NewGuid().ToString();
    
    protected override SetupInMemoryStorage GetStorage()
    {
        return new SetupInMemoryStorage();
    }
    
    protected override InMemoryProjectionContext<int, TestDocument<int>> CreateInsertRequest(SimpleIdContext<int> id)
    {
        return new InMemoryProjectionContext<int, TestDocument<int>>(
            id,
            new TestDocument<int>
            {
                Id = id,
                HandledEvents = ImmutableList.Create(_eventId)
            });
    }

    protected override InMemoryProjectionContext<int, TestDocument<int>> CreateDeleteRequest(SimpleIdContext<int> id)
    {
        return new InMemoryProjectionContext<int, TestDocument<int>>(
            id,
            null);
    }
    
    protected override IProjection<SimpleIdContext<int>, InMemoryProjectionContext<int, TestDocument<int>>, SetupInMemoryStorage> 
        CreateProjection()
    {
        return new TestProjection<int>(ImmutableList<object>.Empty, ImmutableList<StorageFailures>.Empty);
    }

    protected override Task VerifyContext(InMemoryProjectionContext<int, TestDocument<int>> loaded)
    {
        loaded.Document!.HandledEvents.ShouldBe(ImmutableList.Create(_eventId), ignoreOrder: true);

        return Task.CompletedTask;
    }
}