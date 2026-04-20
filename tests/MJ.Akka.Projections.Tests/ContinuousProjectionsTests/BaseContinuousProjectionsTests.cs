using System.Collections.Immutable;
using Akka.Streams;
using AutoFixture;
using MJ.Akka.Projections.Storage;
using Shouldly;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;
using Xunit;

namespace MJ.Akka.Projections.Tests.ContinuousProjectionsTests;

public abstract class BaseContinuousProjectionsTests<TIdContext, TContext, TStorageSetup>(IHaveActorSystem actorSystemHandler)
    where TIdContext : IProjectionIdContext where TContext : IProjectionContext where TStorageSetup : IStorageSetup
{
    protected readonly Fixture Fixture = new();
    protected virtual TimeSpan Timeout => TimeSpan.FromSeconds(5);

    [Fact]
    public async Task Projecting_event_that_fails_once_with_restart_behaviour()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var events = ImmutableList.Create(GetEventThatFails(id, 1));
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithRestartSettings(
                        RestartSettings.Create(
                                TimeSpan.Zero,
                                TimeSpan.Zero,
                                1)
                            .WithMaxRestarts(5, TimeSpan.FromSeconds(10)))
                    .WithProjection(projection))
                .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(1);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();
    }

    [Fact]
    public async Task Projecting_event_that_fails_once_without_restart_behaviour()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var events = ImmutableList.Create(GetEventThatFails(id, 1));
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithProjection(projection))
                .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await Should.ThrowAsync<Exception>(() =>
            coordinator.Get(projection.Name)!.WaitForCompletion(Timeout));

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBeNull();

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeFalse();
    }

    [Fact]
    public async Task Projecting_event_where_storage_fails_once_with_restart_behaviour()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var events = ImmutableList.Create(GetTestEvent(id));
        var failures = ImmutableList.Create(new StorageFailures(
            _ => true,
            _ => false,
            new Exception("Failure")));
        
        var projection = GetProjection(events, failures);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();
        var failureWrapper = new TestFailureStorageWrapper.Modifier(failures);

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithRestartSettings(
                        RestartSettings.Create(
                                TimeSpan.Zero,
                                TimeSpan.Zero,
                                1)
                            .WithMaxRestarts(5, TimeSpan.FromSeconds(10)))
                    .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper)
                    .WithModifiedStorage(failureWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(1);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();
    }

    [Fact]
    public async Task Projecting_where_storage_fails_once_without_restart_behaviour()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var events = ImmutableList.Create(GetTestEvent(id));
        var failures = ImmutableList.Create(new StorageFailures(
            _ => true,
            _ => false,
            new Exception("Failure")));
        
        var projection = GetProjection(events, failures);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();
        var failureWrapper = new TestFailureStorageWrapper.Modifier(failures);

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper)
                    .WithModifiedStorage(failureWrapper),
                storageSetup)
            .Start();

        await Should.ThrowAsync<Exception>(() =>
            coordinator.Get(projection.Name)!.WaitForCompletion(Timeout));

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBeNull();

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeFalse();
    }

    [Fact]
    public async Task Projecting_event_that_fails_once_and_storage_fails_once_with_restart_behaviour()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var events = ImmutableList.Create(GetEventThatFails(id, 1));
        var failures = ImmutableList.Create(new StorageFailures(
            _ => true,
            _ => false,
            new Exception("Failure")));
        
        var projection = GetProjection(events, failures);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();
        var failureWrapper = new TestFailureStorageWrapper.Modifier(failures);

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithRestartSettings(
                        RestartSettings.Create(
                                TimeSpan.Zero,
                                TimeSpan.Zero,
                                1)
                            .WithMaxRestarts(5, TimeSpan.FromSeconds(10)))
                    .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper)
                    .WithModifiedStorage(failureWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(1);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();
    }

    [Fact]
    public async Task Projecting_event_where_storage_load_fails_once_with_restart_behaviour()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var events = ImmutableList.Create(GetTestEvent(id));
        var failures = ImmutableList.Create(new StorageFailures(
            _ => false,
            _ => true,
            new Exception("Failure")));
        
        var projection = GetProjection(events, failures);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();
        var failureWrapper = new TestFailureStorageWrapper.Modifier(failures);

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithRestartSettings(
                        RestartSettings.Create(
                                TimeSpan.Zero,
                                TimeSpan.Zero,
                                1)
                            .WithMaxRestarts(5, TimeSpan.FromSeconds(10)))
                    .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper)
                    .WithModifiedStorage(failureWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(1);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();
    }

    [Fact]
    public async Task Projecting_event_where_storage_load_fails_once_without_restart_behaviour()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var events = ImmutableList.Create(GetTestEvent(id));
        var failures = ImmutableList.Create(new StorageFailures(
            _ => false,
            _ => true,
            new Exception("Failure")));
        
        var projection = GetProjection(events, failures);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();
        var failureWrapper = new TestFailureStorageWrapper.Modifier(failures);

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper)
                    .WithModifiedStorage(failureWrapper),
                storageSetup)
            .Start();

        await Should.ThrowAsync<Exception>(() =>
            coordinator.Get(projection.Name)!.WaitForCompletion(Timeout));

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBeNull();

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeFalse();
    }

    [Fact]
    public async Task Projecting_transformation_to_two_events_for_one_document()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var firstEvent = GetTestEvent(id);
        var secondEvent = GetTestEvent(id);
        var transformEvent = GetTransformationEvent(id, ImmutableList.Create(firstEvent, secondEvent));

        var events = ImmutableList.Create(transformEvent);
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(1);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();

        await VerifyContext(id, context, events, projection);
    }

    [Fact]
    public async Task Projecting_transformation_to_two_events_for_two_documents()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var firstId = Fixture.Create<TIdContext>();
        var secondId = Fixture.Create<TIdContext>();

        var firstEvent = GetTestEvent(firstId);
        var secondEvent = GetTestEvent(secondId);
        var transformEvent = GetTransformationEvent(
            Fixture.Create<TIdContext>(),
            ImmutableList.Create(firstEvent, secondEvent));

        var events = ImmutableList.Create(transformEvent);
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(1);

        var firstContext = await loader.Load(firstId, projection.GetDefaultContext);

        firstContext.Exists().ShouldBeTrue();

        await VerifyContext(firstId, firstContext, events, projection);

        var secondContext = await loader.Load(secondId, projection.GetDefaultContext);

        secondContext.Exists().ShouldBeTrue();

        await VerifyContext(secondId, secondContext, events, projection);
    }

    [Fact]
    public async Task Projecting_two_events_that_doesnt_match_projection()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var events = ImmutableList.Create(
            GetUnMatchedEvent(id),
            GetUnMatchedEvent(id));
        
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(2);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeFalse();
    }

    [Fact]
    public async Task Projecting_one_matched_and_one_unmatched_event()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var events = ImmutableList.Create(
            GetUnMatchedEvent(id),
            GetTestEvent(id));
        
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                        .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(2);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();
        
        await VerifyContext(id, context, events, projection);
    }

    [Fact]
    public async Task Projecting_one_event_for_one_document()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var firstEvent = GetTestEvent(id);

        var events = ImmutableList.Create(firstEvent);
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(1);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();

        await VerifyContext(id, context, events, projection);
    }

    [Fact]
    public async Task Projecting_two_events_for_one_document()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var firstEvent = GetTestEvent(id);
        var secondEvent = GetTestEvent(id);

        var events = ImmutableList.Create(firstEvent, secondEvent);
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(2);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();

        await VerifyContext(id, context, events, projection);
    }

    [Fact]
    public async Task Projecting_two_events_for_two_documents()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var firstId = Fixture.Create<TIdContext>();
        var secondId = Fixture.Create<TIdContext>();

        var firstEvent = GetTestEvent(firstId);
        var secondEvent = GetTestEvent(secondId);

        var events = ImmutableList.Create(firstEvent, secondEvent);
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(2);

        var firstContext = await loader.Load(firstId, projection.GetDefaultContext);

        firstContext.Exists().ShouldBeTrue();

        await VerifyContext(firstId, firstContext, events, projection);

        var secondContext = await loader.Load(secondId, projection.GetDefaultContext);

        secondContext.Exists().ShouldBeTrue();

        await VerifyContext(secondId, secondContext, events, projection);
    }

    [Fact]
    public async Task Projecting_from_second_event_position()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var firstEvent = GetTestEvent(id);
        var secondEvent = GetTestEvent(id);

        var events = ImmutableList.Create(firstEvent, secondEvent);
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var projectionsSetup = system
            .Projections(config => Configure(config
                    .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup);

        await storageWrapper.Wrapper.PositionStorage.StoreLatestPosition(projection.Name, 1);

        var coordinator = await projectionsSetup.Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(2);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();

        await VerifyContext(id, context, ImmutableList.Create(secondEvent), projection);
    }

    [Fact]
    public async Task Projecting_from_initial_position_after_first_event()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var firstEvent = GetTestEvent(id);
        var secondEvent = GetTestEvent(id);

        var events = ImmutableList.Create(firstEvent, secondEvent);
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty, 1);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var projectionsSetup = system
            .Projections(config => Configure(config
                        .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup);
        
        var coordinator = await projectionsSetup.Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(2);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();

        await VerifyContext(id, context, ImmutableList.Create(secondEvent), projection);
    }

    [Fact]
    public async Task Projecting_three_events_in_same_group()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var documentId = Fixture.Create<TIdContext>();

        var events = ImmutableList.Create(
            GetTestEvent(documentId),
            GetTestEvent(documentId),
            GetTestEvent(documentId));

        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                    .WithProjection(projection))
                .WithEventBatchingStrategy(
                    new BatchWithinEventBatchingStrategy(3, TimeSpan.FromSeconds(1)))
                .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(3);

        var context = await loader.Load(documentId, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();

        await VerifyContext(documentId, context, events, projection);
    }
    
    [Fact]
    public async Task Projecting_six_events_in_two_groups()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var documentId = Fixture.Create<TIdContext>();

        var events = ImmutableList.Create(
            GetTestEvent(documentId),
            GetTestEvent(documentId),
            GetTestEvent(documentId),
            GetTestEvent(documentId),
            GetTestEvent(documentId),
            GetTestEvent(documentId));

        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                        .WithProjection(projection))
                    .WithEventBatchingStrategy(
                        new BatchWithinEventBatchingStrategy(3, TimeSpan.FromSeconds(1)))
                    .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(6);

        var context = await loader.Load(documentId, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();

        await VerifyContext(documentId, context, events, projection);
    }
    
    [Fact]
    public async Task Projecting_six_events_in_two_groups_where_two_events_are_unhandled()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var documentId = Fixture.Create<TIdContext>();

        var events = ImmutableList.Create(
            GetTestEvent(documentId),
            GetUnMatchedEvent(documentId),
            GetTestEvent(documentId),
            GetTestEvent(documentId),
            GetUnMatchedEvent(documentId),
            GetTestEvent(documentId));

        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                        .WithProjection(projection))
                    .WithEventBatchingStrategy(
                        new BatchWithinEventBatchingStrategy(3, TimeSpan.FromSeconds(1)))
                    .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(6);

        var context = await loader.Load(documentId, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();

        await VerifyContext(documentId, context, events, projection);
    }
    
    [Fact]
    public async Task Projecting_three_events_in_same_group_where_one_is_un_matched()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var documentId = Fixture.Create<TIdContext>();

        var events = ImmutableList.Create(
            GetTestEvent(documentId),
            GetUnMatchedEvent(documentId),
            GetTestEvent(documentId));

        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                        .WithProjection(projection))
                    .WithEventBatchingStrategy(
                        new BatchWithinEventBatchingStrategy(3, TimeSpan.FromSeconds(1)))
                    .WithPositionStorageBatchingStrategy(new NoBatchingPositionStrategy())
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(3);

        var context = await loader.Load(documentId, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();

        await VerifyContext(documentId, context, events, projection);
    }

    [Fact]
    public async Task Projecting_ten_events_that_are_filtered_out()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var events = Enumerable.Range(1, 10)
            .Select(_ => GetEventThatIsFilteredOut(id))
            .ToImmutableList();
        
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                        .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(10);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeFalse();
    }
    
    [Fact]
    public async Task Projecting_ten_events_without_document_id()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();

        var events = Enumerable.Range(1, 10)
            .Select(_ => GetEventThatDoesntGetDocumentId(id))
            .ToImmutableList();
        
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                        .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(10);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeFalse();
    }

    [Fact]
    public async Task Projecting_two_events_that_transform_into_no_events()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();
        
        var projection = GetProjection([
            GetTransformationEvent(id, ImmutableList<object>.Empty), 
            GetTransformationEvent(id, ImmutableList<object>.Empty)], 
            ImmutableList<StorageFailures>.Empty);
        
        var storageSetup = CreateStorageSetup();
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                        .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(2);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeFalse();
    }

    [Fact]
    public async Task Projecting_event_with_data_used_for_getting_id()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();
        var expectedData = Fixture.Create<string>();

        var events = ImmutableList.Create(GetEventWithDataForId(id, expectedData));
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();

        var loader = projection.GetLoadProjectionContext(storageSetup);

        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                        .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(1);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();

        await VerifyDataContext(id, context, expectedData);
    }

    [Fact]
    public async Task Projecting_event_with_data_forwarded_to_handler()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();
        var expectedData = Fixture.Create<string>();

        var events = ImmutableList.Create(GetEventWithDataForHandler(id, expectedData));
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();

        var loader = projection.GetLoadProjectionContext(storageSetup);

        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                        .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(1);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();

        await VerifyDataContext(id, context, expectedData);
    }

    [Fact]
    public async Task Projecting_event_with_data_used_in_transform()
    {
        using var system = actorSystemHandler.StartNewActorSystem();

        var id = Fixture.Create<TIdContext>();
        var expectedData = Fixture.Create<string>();
        var transformedEvent = GetTestEvent(id);

        var events = ImmutableList.Create(
            GetEventWithDataForTransform(id, expectedData, ImmutableList.Create(transformedEvent)));
        var projection = GetProjection(events, ImmutableList<StorageFailures>.Empty);
        var storageSetup = CreateStorageSetup();

        var loader = projection.GetLoadProjectionContext(storageSetup);

        var storageWrapper = new TestStorageWrapper.Modifier();

        var coordinator = await system
            .Projections(config => Configure(config
                        .WithProjection(projection))
                    .WithModifiedStorage(storageWrapper),
                storageSetup)
            .Start();

        await coordinator.Get(projection.Name)!.WaitForCompletion(Timeout);

        var position = await storageWrapper.Wrapper.PositionStorage.LoadLatestPosition(projection.Name);

        position.ShouldBe(1);

        var context = await loader.Load(id, projection.GetDefaultContext);

        context.Exists().ShouldBeTrue();

        await VerifyContext(id, context, ImmutableList.Create(transformedEvent), projection);
    }

    protected virtual IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> Configure(
        IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> config)
    {
        return config;
    }

    protected abstract TStorageSetup CreateStorageSetup();
    
    protected abstract IProjection<TIdContext, TContext, TStorageSetup> GetProjection(
        IImmutableList<object> events,
        IImmutableList<StorageFailures> storageFailures,
        long? initialPosition = null);

    protected abstract object GetEventThatFails(TIdContext id, int numberOfFailures);

    protected abstract object GetTestEvent(TIdContext documentId);

    protected abstract object GetTransformationEvent(TIdContext documentId, IImmutableList<object> transformTo);

    protected abstract object GetUnMatchedEvent(TIdContext documentId);
    
    protected abstract object GetEventThatIsFilteredOut(TIdContext documentId);
    
    protected abstract object GetEventThatDoesntGetDocumentId(TIdContext documentId);

    protected abstract object GetEventWithDataForId(TIdContext documentId, string data);

    protected abstract object GetEventWithDataForHandler(TIdContext documentId, string data);

    protected abstract object GetEventWithDataForTransform(TIdContext documentId, string data, IImmutableList<object> transformTo);

    protected abstract Task VerifyDataContext(TIdContext documentId, TContext context, string expectedData);

    protected abstract Task VerifyContext(
        TIdContext documentId,
        TContext context,
        IImmutableList<object> events,
        IProjection projection);
}




