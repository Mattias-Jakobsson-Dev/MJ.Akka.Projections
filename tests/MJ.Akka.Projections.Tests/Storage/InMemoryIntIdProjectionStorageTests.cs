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
    protected override SetupInMemoryStorage GetStorage()
    {
        return new SetupInMemoryStorage();
    }

    protected override InMemoryProjectionContext<int, TestDocument<int>> CreateTestContext(int id)
    {
        return new InMemoryProjectionContext<int, TestDocument<int>>(id, new TestDocument<int>
        {
            Id = id,
            HandledEvents = ImmutableList.Create(Guid.NewGuid().ToString())
        });
    }

    protected override InMemoryProjectionContext<int, TestDocument<int>> Delete(
        InMemoryProjectionContext<int, TestDocument<int>> context)
    {
        context.ModifyDocument(_ => null);

        return context;
    }

    protected override IProjection<int, InMemoryProjectionContext<int, TestDocument<int>>, SetupInMemoryStorage> 
        CreateProjection()
    {
        return new TestProjection<int>(ImmutableList<object>.Empty, ImmutableList<StorageFailures>.Empty);
    }

    protected override Task VerifyContext(
        InMemoryProjectionContext<int, TestDocument<int>> original, 
        InMemoryProjectionContext<int, TestDocument<int>> loaded)
    {
        loaded.Id.Should().Be(original.Id);
        
        loaded.Document!.HandledEvents.Should().BeEquivalentTo(original.Document!.HandledEvents);

        return Task.CompletedTask;
    }
}