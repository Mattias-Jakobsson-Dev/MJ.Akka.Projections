using System.Collections.Immutable;
using Akka.Streams;
using Akka.TestKit.Extensions;
using AutoFixture;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.OneTime;
using FluentAssertions;
using Xunit;

namespace DC.Akka.Projections.Tests.OnTimeProjectionsTests;

public abstract class BaseOneTimeProjectionsTest<TId, TDocument>(IHaveActorSystem actorSystemHandler)
    where TId : notnull
    where TDocument : notnull
{
    protected readonly Fixture Fixture = new();

    [Fact]
    public async Task Projecting_event_that_fails_once_with_restart_behaviour()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TId>();

        var events = ImmutableList.Create(GetEventThatFails(id, 1));
        var projection = GetProjection(events);

        var result = await system
            .CreateOneTimeProjection(
                projection,
                x => Configure(x)
                    .WithRestartSettings(
                        RestartSettings.Create(
                                TimeSpan.Zero,
                                TimeSpan.Zero,
                                1)
                            .WithMaxRestarts(5, TimeSpan.FromSeconds(10))))
            .Run(TimeSpan.FromSeconds(5));

        var document = await result.Load(id);

        document.Should().NotBeNull();
    }

    [Fact]
    public async Task Projecting_event_that_fails_once_without_restart_behaviour()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TId>();

        var events = ImmutableList.Create(GetEventThatFails(id, 1));
        var projection = GetProjection(events);

        await system
            .CreateOneTimeProjection(
                projection,
                Configure)
            .Run(TimeSpan.FromSeconds(5))
            .ShouldThrowWithin<Exception>(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Projecting_transformation_to_two_events_for_one_document()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TId>();

        var firstEvent = GetTestEvent(id);
        var secondEvent = GetTestEvent(id);
        var transformEvent = GetTransformationEvent(id, ImmutableList.Create(firstEvent, secondEvent));

        var events = ImmutableList.Create(transformEvent);
        var projection = GetProjection(events);

        var result = await system
            .CreateOneTimeProjection(projection, Configure)
            .Run(TimeSpan.FromSeconds(5));

        var document = await result.Load(id);

        document.Should().NotBeNull();

        await VerifyDocument(id, document!, events);
    }

    [Fact]
    public async Task Projecting_transformation_to_two_events_for_two_documents()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var firstId = Fixture.Create<TId>();
        var secondId = Fixture.Create<TId>();

        var firstEvent = GetTestEvent(firstId);
        var secondEvent = GetTestEvent(secondId);
        var transformEvent = GetTransformationEvent(
            Fixture.Create<TId>(),
            ImmutableList.Create(firstEvent, secondEvent));

        var events = ImmutableList.Create(transformEvent);
        var projection = GetProjection(events);

        var result = await system
            .CreateOneTimeProjection(projection, Configure)
            .Run(TimeSpan.FromSeconds(5));

        var firstDocument = await result.Load(firstId);

        firstDocument.Should().NotBeNull();

        await VerifyDocument(firstId, firstDocument!, events);

        var secondDocument = await result.Load(secondId);

        secondDocument.Should().NotBeNull();

        await VerifyDocument(secondId, secondDocument!, events);
    }

    [Fact]
    public async Task Projecting_two_events_that_doesnt_match_projection()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TId>();

        var events = ImmutableList.Create(
            GetUnMatchedEvent(id),
            GetUnMatchedEvent(id));

        var projection = GetProjection(events);

        var result = await system
            .CreateOneTimeProjection(projection, Configure)
            .Run(TimeSpan.FromSeconds(5));

        var document = await result.Load(id);

        document.Should().BeNull();
    }

    [Fact]
    public async Task Projecting_two_events_for_one_document()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TId>();

        var firstEvent = GetTestEvent(id);
        var secondEvent = GetTestEvent(id);

        var events = ImmutableList.Create(firstEvent, secondEvent);
        var projection = GetProjection(events);

        var result = await system
            .CreateOneTimeProjection(projection, Configure)
            .Run(TimeSpan.FromSeconds(5));

        var document = await result.Load(id);

        document.Should().NotBeNull();

        await VerifyDocument(id, document!, events);
    }

    [Fact]
    public async Task Projecting_two_events_for_two_documents()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var firstId = Fixture.Create<TId>();
        var secondId = Fixture.Create<TId>();

        var firstEvent = GetTestEvent(firstId);
        var secondEvent = GetTestEvent(secondId);

        var events = ImmutableList.Create(firstEvent, secondEvent);
        var projection = GetProjection(events);

        var result = await system
            .CreateOneTimeProjection(projection, Configure)
            .Run(TimeSpan.FromSeconds(5));

        var firstDocument = await result.Load(firstId);

        firstDocument.Should().NotBeNull();

        await VerifyDocument(firstId, firstDocument!, events);

        var secondDocument = await result.Load(secondId);

        secondDocument.Should().NotBeNull();

        await VerifyDocument(secondId, secondDocument!, events);
    }

    [Fact]
    public async Task Projecting_from_second_event_position()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TId>();

        var firstEvent = GetTestEvent(id);
        var secondEvent = GetTestEvent(id);

        var events = ImmutableList.Create(firstEvent, secondEvent);
        var projection = GetProjection(events);

        var result = await system
            .CreateOneTimeProjection(
                projection,
                x => Configure(x)
                    .StartFrom(1))
            .Run(TimeSpan.FromSeconds(5));

        var document = await result.Load(id);

        document.Should().NotBeNull();

        await VerifyDocument(id, document!, ImmutableList.Create(secondEvent));
    }

    protected virtual IHaveConfiguration<OneTimeProjectionConfig> Configure(
        IHaveConfiguration<OneTimeProjectionConfig> config)
    {
        return config;
    }

    protected abstract IProjection<TId, TDocument> GetProjection(IImmutableList<object> events);

    protected abstract object GetEventThatFails(TId id, int numberOfFailures);

    protected abstract object GetTestEvent(TId documentId);

    protected abstract object GetTransformationEvent(TId documentId, IImmutableList<object> transformTo);

    protected abstract object GetUnMatchedEvent(TId documentId);

    protected abstract Task VerifyDocument(TId documentId, TDocument document, IImmutableList<object> events);
}