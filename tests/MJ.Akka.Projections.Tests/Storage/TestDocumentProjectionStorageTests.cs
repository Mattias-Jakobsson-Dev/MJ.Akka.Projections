using System.Collections.Immutable;
using FluentAssertions;
using MJ.Akka.Projections.Tests.TestData;

namespace MJ.Akka.Projections.Tests.Storage;

public abstract class TestDocumentProjectionStorageTests<TId> : ProjectionStorageTests<TId, TestDocument<TId>>
    where TId : notnull
{
    protected override TestDocument<TId> CreateTestDocument(TId id)
    {
        return new TestDocument<TId>
        {
            Id = id,
            HandledEvents = ImmutableList.Create(Guid.NewGuid().ToString())
        };
    }

    protected override Task VerifyDocument(TestDocument<TId> original, TestDocument<TId> loaded)
    {
        loaded.Id.Should().Be(original.Id);
        loaded.HandledEvents.Should().BeEquivalentTo(original.HandledEvents);
        
        return Task.CompletedTask;
    }
}