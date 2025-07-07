using System.Collections.Immutable;
using Akka.Streams;
using Akka.TestKit.Xunit2;
using AutoFixture;
using FluentAssertions;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.Storage.Batched;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Tests.TestData;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public class BatchedStorageTests : TestKit
{
    private readonly Fixture _fixture = new();

    [Fact]
    public async Task Projecting_events_when_storage_fails_once_with_restart_settings()
    {
        var id = _fixture.Create<string>();
        var eventId = _fixture.Create<string>();

        var events = ImmutableList.Create<object>(new Events<string>.FirstEvent(id, eventId));
        var failures = ImmutableList.Create(new StorageFailures(
            _ => true,
            _ => false,
            new Exception("Failure")));

        var projection = new TestProjection<string>(events, failures);
        var storageSetup = new SetupInMemoryStorage();

        var loader = projection.GetLoadProjectionContext(storageSetup);

        var storageWrapper = new TestStorageWrapper.Modifier();
        var failureWrapper = new TestFailureStorageWrapper.Modifier(failures);

        var coordinator = await Sys
            .Projections(config => config
                    .WithRestartSettings(
                        RestartSettings.Create(
                                TimeSpan.Zero,
                                TimeSpan.Zero,
                                1)
                            .WithMaxRestarts(5, TimeSpan.FromSeconds(10)))
                    .WithProjection(projection)
                    .WithBatchedStorage()
                    .WithModifiedStorage(storageWrapper)
                    .WithModifiedStorage(failureWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(TimeSpan.FromSeconds(5));

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.Should().Be(1);

        var context = await loader.Load(id);

        context.Exists().Should().BeTrue();

        context.Document!.HandledEvents.Should().HaveCount(1);

        context.Document!.HandledEvents[0].Should().Be(eventId);
    }
}