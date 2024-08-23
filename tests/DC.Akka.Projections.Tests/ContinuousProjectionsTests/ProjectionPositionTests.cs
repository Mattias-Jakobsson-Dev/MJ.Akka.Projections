using System.Collections.Immutable;
using Akka.TestKit.Extensions;
using Akka.TestKit.Xunit2;
using AutoFixture;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using Xunit;

namespace DC.Akka.Projections.Tests.ContinuousProjectionsTests;

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

        var projection = new TestProjection<string>(events);

        IProjectionPositionStorage positionStorage = null!;

        var coordinator = await Sys
            .Projections(config => config
                .WithProjection(projection)
                .WithEventBatchingStrategy(new NoEventBatchingStrategy(1))
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                .WithModifiedConfig(conf =>
                {
                    positionStorage = conf.PositionStorage!;

                    return conf;
                }))
            .Start();

        await coordinator
            .Get(projection.Name)!
            .WaitForCompletion(TimeSpan.FromSeconds(5))
            .ShouldThrowWithin<Exception>(TimeSpan.FromSeconds(5));

        var position = await positionStorage.LoadLatestPosition(projection.Name);

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

        var projection = new TestProjection<string>(events);

        IProjectionStorage projectionStorage = null!;
        IProjectionPositionStorage positionStorage = null!;

        var coordinator = await Sys
            .Projections(config => config
                .WithProjection(projection)
                .WithEventBatchingStrategy(new NoEventBatchingStrategy(1))
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                .WithModifiedConfig(conf =>
                {
                    projectionStorage = conf.ProjectionStorage!;
                    positionStorage = conf.PositionStorage!;

                    return conf;
                }))
            .Start();

        await coordinator
            .Get(projection.Name)!
            .WaitForCompletion(TimeSpan.FromSeconds(5))
            .ShouldThrowWithin<Exception>(TimeSpan.FromSeconds(5));

        var position = await positionStorage.LoadLatestPosition(projection.Name);

        position.Should().BeLessThan(3);

        var firstDocument = await projectionStorage.LoadDocument<TestDocument<string>>(documentId);

        firstDocument.Should().NotBeNull();

        firstDocument!.HandledEvents.Should().HaveCount(2);
        firstDocument.HandledEvents.Should().BeEquivalentTo(ImmutableList.Create(firstEventId, secondEventId));
    }
}