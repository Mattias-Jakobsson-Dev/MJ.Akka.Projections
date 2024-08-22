using System.Collections.Immutable;
using Akka.Streams;
using Akka.TestKit.Xunit2;
using AutoFixture;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using Xunit;

namespace DC.Akka.Projections.Tests.ContinuousProjectionsTests;

public class BatchedStorageTests : TestKit
{
    private readonly Fixture _fixture = new();
    
    [Fact]
    public async Task Projecting_events_when_storage_fails_once_with_restart_settings()
    {
        var id = _fixture.Create<string>();
        var eventId = _fixture.Create<string>();

        var events = ImmutableList.Create<object>(new Events<string>.FirstEvent(id, eventId));
        var projection = new TestProjection<string>(events);

        var projectionStorage = new FailOnceStorage();
        var positionStorage = new InMemoryPositionStorage();

        var coordinator = await Sys
            .Projections(config => config
                .WithRestartSettings(
                    RestartSettings.Create(
                            TimeSpan.Zero,
                            TimeSpan.Zero,
                            1)
                        .WithMaxRestarts(5, TimeSpan.FromSeconds(10)))
                .WithProjectionStreamConfiguration(ProjectionStreamConfiguration.Default with
                {
                    MaxProjectionRetries = 0
                })
                .WithProjectionStorage(projectionStorage)
                .Batched()
                .WithPositionStorage(positionStorage)
                .WithProjection(projection))
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(TimeSpan.FromSeconds(5));

        var position = await positionStorage.LoadLatestPosition(projection.Name);

        position.Should().Be(1);

        var document = await projectionStorage.LoadDocument<TestDocument<string>>(id);

        document.Should().NotBeNull();

        document!.HandledEvents.Should().HaveCount(1);

        document.HandledEvents[0].Should().Be(eventId);
    }
    
    private class FailOnceStorage : InMemoryProjectionStorage
    {
        private readonly object _lock = new { };
        private bool _hasFailed;
        
        public override async Task Store(
            IImmutableList<DocumentToStore> toUpsert,
            IImmutableList<DocumentToDelete> toDelete, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_hasFailed)
                {
                    _hasFailed = true;

                    throw new Exception("Storage failed");
                }
            }

            await base.Store(toUpsert, toDelete, cancellationToken);
        }
    }
}