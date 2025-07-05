using System.Collections.Immutable;
using Akka.TestKit.Extensions;
using Akka.TestKit.Xunit2;
using AutoFixture;
using FluentAssertions;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public class ProjectionPositionTests : TestKit
{
    private readonly Fixture _fixture = new();

    [Fact]
    public async Task Ensure_correct_position_when_event_in_middle_fails()
    {
        var firstDocumentId = _fixture.Create<string>();
        var secondDocumentId = _fixture.Create<string>();

        var events = ImmutableList.Create<object>(
            new Events<string>.FirstEvent(firstDocumentId, _fixture.Create<string>()),
            new Events<string>.FailProjection(
                secondDocumentId,
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                1,
                new Exception("Failed")),
            new Events<string>.FirstEvent(firstDocumentId, _fixture.Create<string>()));

        var projection = new TestProjection<string>(events, ImmutableList<StorageFailures>.Empty);

        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await Sys
            .Projections(config => config
                    .WithProjection(projection)
                    .WithEventBatchingStrategy(new NoEventBatchingStrategy(1))
                    .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                    .WithModifiedStorage(storageWrapper),
                new SetupInMemoryStorage())
            .Start();

        await coordinator
            .Get(projection.Name)!
            .WaitForCompletion(TimeSpan.FromSeconds(5))
            .ShouldThrowWithin<Exception>(TimeSpan.FromSeconds(5));

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        (position ?? 0).Should().BeLessThan(2);
    }

    [Fact]
    public async Task Ensure_correct_position_when_third_event_fails()
    {
        var documentId = _fixture.Create<string>();
        var firstEventId = _fixture.Create<string>();
        var secondEventId = _fixture.Create<string>();

        var events = ImmutableList.Create<object>(
            new Events<string>.FirstEvent(documentId, firstEventId),
            new Events<string>.FirstEvent(documentId, secondEventId),
            new Events<string>.FailProjection(
                documentId,
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                1,
                new Exception("Failed")),
            new Events<string>.FirstEvent(documentId, _fixture.Create<string>()),
            new Events<string>.FirstEvent(documentId, _fixture.Create<string>()));

        var projection = new TestProjection<string>(events, ImmutableList<StorageFailures>.Empty);

        var storageWrapper = new TestStorageWrapper.Modifier();

        var storageSetup = new SetupInMemoryStorage();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);

        var coordinator = await Sys
            .Projections(config => config
                    .WithProjection(projection)
                    .WithEventBatchingStrategy(new NoEventBatchingStrategy(1))
                    .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator
            .Get(projection.Name)!
            .WaitForCompletion(TimeSpan.FromSeconds(5))
            .ShouldThrowWithin<Exception>(TimeSpan.FromSeconds(5));

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.Should().BeLessThan(3);

        var firstContext = await loader.Load(documentId);

        firstContext.Exists().Should().BeTrue();
    }
}