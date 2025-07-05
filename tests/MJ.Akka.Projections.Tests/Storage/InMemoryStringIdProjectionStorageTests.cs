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
    protected override SetupInMemoryStorage GetStorage()
    {
        return new SetupInMemoryStorage();
    }

    protected override InMemoryProjectionContext<string, TestDocument<string>> CreateTestContext(string id)
    {
        return new InMemoryProjectionContext<string, TestDocument<string>>(id, new TestDocument<string>
        {
            Id = id,
            HandledEvents = ImmutableList.Create(Guid.NewGuid().ToString())
        });
    }

    protected override InMemoryProjectionContext<string, TestDocument<string>> Delete(
        InMemoryProjectionContext<string, TestDocument<string>> context)
    {
        context.ModifyDocument(_ => null);

        return context;
    }

    protected override IProjection<string, InMemoryProjectionContext<string, TestDocument<string>>, SetupInMemoryStorage> 
        CreateProjection()
    {
        return new TestProjection<string>(ImmutableList<object>.Empty, ImmutableList<StorageFailures>.Empty);
    }

    protected override Task VerifyContext(
        InMemoryProjectionContext<string, TestDocument<string>> original, 
        InMemoryProjectionContext<string, TestDocument<string>> loaded)
    {
        loaded.Id.Should().Be(original.Id);
        
        loaded.Document!.HandledEvents.Should().BeEquivalentTo(original.Document!.HandledEvents);

        return Task.CompletedTask;
    }
}