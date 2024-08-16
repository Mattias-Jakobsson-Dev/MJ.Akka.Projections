using System.Collections.Immutable;
using Akka.Streams;
using AutoFixture;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace DC.Akka.Projections.Tests.ProjectionCoordinator;

public class When_projecting_event_that_fails_once_with_restart_behaviour(
    When_projecting_event_that_fails_once_with_restart_behaviour.TestFixture fixture)
    : IClassFixture<When_projecting_event_that_fails_once_with_restart_behaviour.TestFixture>
{
    [Fact]
    public async Task Then_position_should_be_correct()
    {
        var position = await fixture.LoadPosition(TestProjection<string>.GetName());

        position.Should().Be(1);
    }

    [Fact]
    public async Task Then_document_should_be_saved()
    {
        var doc = await fixture.LoadDocument<TestDocument<string>>(fixture.DocumentId);

        doc.Should().NotBeNull();
    }

    [Fact]
    public async Task Exactly_one_document_should_be_saved()
    {
        var docs = await fixture.LoadAllDocuments();

        docs.Should().HaveCount(1);
    }

    [Fact]
    public async Task Then_document_should_have_added_one_event()
    {
        var doc = await fixture.LoadDocument<TestDocument<string>>(fixture.DocumentId);

        doc!.HandledEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task Then_document_should_have_added_event()
    {
        var doc = await fixture.LoadDocument<TestDocument<string>>(fixture.DocumentId);

        doc!.HandledEvents[0].Should().Be(fixture.EventId);
    }

    [Fact]
    public void Then_projection_should_complete_successfully()
    {
        fixture
            .GetExceptionFor(TestProjection<string>.GetName())
            .Should()
            .BeNull();
    }

    [PublicAPI]
    public class TestFixture : ProjectionCoordinatorTestsBase
    {
        public string DocumentId { get; } = new Fixture().Create<string>();
        public string EventId { get; } = Guid.NewGuid().ToString();

        protected override IProjectionsSetup Configure(IProjectionsSetup setup)
        {
            return setup
                .WithRestartSettings(RestartSettings.Create(
                        TimeSpan.Zero,
                        TimeSpan.Zero,
                        1)
                    .WithMaxRestarts(5, TimeSpan.FromSeconds(10)))
                .WithProjectionStreamConfiguration(ProjectionStreamConfiguration.Default with
                {
                    MaxProjectionRetries = 0
                })
                .WithTestProjection<string>(
                    ImmutableList.Create<object>(
                        new Events<string>.FailProjection(
                            DocumentId,
                            EventId,
                            Guid.NewGuid().ToString(),
                            1,
                            new Exception("Projection failed"))));
        }
    }
}