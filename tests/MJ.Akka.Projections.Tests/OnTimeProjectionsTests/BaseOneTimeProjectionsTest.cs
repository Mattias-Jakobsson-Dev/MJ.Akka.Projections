using System.Collections.Immutable;
using Akka.Streams;
using Akka.TestKit.Extensions;
using AutoFixture;
using FluentAssertions;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.OneTime;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage.InMemory;
using Xunit;

namespace MJ.Akka.Projections.Tests.OnTimeProjectionsTests;

public abstract class BaseOneTimeProjectionsTest<TId, TDocument>(IHaveActorSystem actorSystemHandler)
    where TId : notnull
    where TDocument : class
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
    public async Task Projecting_two_events_for_two_projections_with_different_names()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TId>();

        var firstEvent = GetTestEvent(id);
        var secondEvent = GetTestEvent(id);

        var events = ImmutableList.Create(firstEvent, secondEvent);
        var firstProjection = GetProjection(events);

        var firstResult = await system
            .CreateOneTimeProjection(firstProjection, Configure)
            .Run(TimeSpan.FromSeconds(5));

        var firstDocument = await firstResult.Load(id);

        firstDocument.Should().NotBeNull();

        await VerifyDocument(id, firstDocument!, events);
        
        var secondProjection = GetSecondaryProjection(events);
        
        var secondResult = await system
            .CreateOneTimeProjection(secondProjection, Configure)
            .Run(TimeSpan.FromSeconds(5));

        var secondDocument = await secondResult.Load(id);

        secondDocument.Should().NotBeNull();

        await VerifyDocument(id, secondDocument!, events);
    }
    
    [Fact]
    public async Task Projecting_two_events_for_two_projections_with_same_name()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TId>();

        var firstEvent = GetTestEvent(id);
        var secondEvent = GetTestEvent(id);

        var events = ImmutableList.Create(firstEvent, secondEvent);
        var firstProjection = GetProjection(events);

        var firstResult = await system
            .CreateOneTimeProjection(firstProjection, Configure)
            .Run(TimeSpan.FromSeconds(5));

        var firstDocument = await firstResult.Load(id);

        firstDocument.Should().NotBeNull();

        await VerifyDocument(id, firstDocument!, events);
        
        var secondProjection = GetProjection(events);
        
        var secondResult = await system
            .CreateOneTimeProjection(secondProjection, Configure)
            .Run(TimeSpan.FromSeconds(5));

        var secondDocument = await secondResult.Load(id);

        secondDocument.Should().NotBeNull();

        await VerifyDocument(id, secondDocument!, events);
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

    protected abstract IProjection<SimpleIdContext<TId>, InMemoryProjectionContext<SimpleIdContext<TId>, TDocument>, SetupInMemoryStorage> GetProjection(
        IImmutableList<object> events);
    
    protected abstract IProjection<SimpleIdContext<TId>, InMemoryProjectionContext<SimpleIdContext<TId>, TDocument>, SetupInMemoryStorage> GetSecondaryProjection(
        IImmutableList<object> events);

    protected abstract object GetEventThatFails(TId id, int numberOfFailures);

    protected abstract object GetTestEvent(TId documentId);

    protected abstract object GetTransformationEvent(TId documentId, IImmutableList<object> transformTo);

    protected abstract object GetUnMatchedEvent(TId documentId);

    protected abstract Task VerifyDocument(TId documentId, TDocument document, IImmutableList<object> events);
}