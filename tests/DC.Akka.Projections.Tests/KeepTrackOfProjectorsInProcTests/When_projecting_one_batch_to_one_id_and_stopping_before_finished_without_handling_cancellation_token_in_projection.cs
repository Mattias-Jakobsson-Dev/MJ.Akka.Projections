using System.Collections.Immutable;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Akka.Util;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.InProc;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace DC.Akka.Projections.Tests.KeepTrackOfProjectorsInProcTests;

public class When_projecting_one_batch_to_one_id_and_stopping_before_finished_without_handling_cancellation_token_in_projection(
    When_projecting_one_batch_to_one_id_and_stopping_before_finished_without_handling_cancellation_token_in_projection.Fixture fixture)
    : IClassFixture<When_projecting_one_batch_to_one_id_and_stopping_before_finished_without_handling_cancellation_token_in_projection.Fixture>
{
    [Fact]
    public void Then_response_should_be_rejection()
    {
        fixture.Response.Should().BeOfType<Messages.Reject>();
    }
    
    [Fact]
    public void Then_projector_should_still_be_running()
    {
        fixture.Projector.Should().NotBeNull();
    }
    
    [PublicAPI]
    public class Fixture : TestKit, IAsyncLifetime
    {
        public Messages.IProjectEventsResponse Response { get; private set; } = null!;
        public IActorRef? Projector { get; private set; }
        
        public async Task InitializeAsync()
        {
            var id = Guid.NewGuid().ToString();
            
            var factory = new KeepTrackOfProjectorsInProc(Sys, new MaxNumberOfProjectorsPassivation(10));

            var projection = new TestProjection<string>(ImmutableList<object>.Empty);
            
            var projectionConfiguration = new ProjectionConfiguration<string, TestDocument<string>>(
                projection,
                new InMemoryProjectionStorage(),
                new InMemoryPositionStorage(),
                factory,
                null,
                BatchEventBatchingStrategy.Default,
                BatchWithinEventPositionBatchingStrategy.Default,
                projection.Configure(new SetupProjection<string, TestDocument<string>>()).Build());
            
            ProjectionConfigurationsSupplier.Register(
                Sys,
                new Dictionary<string, ProjectionConfiguration>
                {
                    [projection.Name] = projectionConfiguration
                }.ToImmutableDictionary());
            
            var projector = await factory.GetProjector<string, TestDocument<string>>(id, projectionConfiguration);
            
            var projectorTask = projector
                .ProjectEvents(
                    ImmutableList.Create(new EventWithPosition(
                        new Events<string>.DelayHandlingWithoutCancellationToken(
                            id,
                            Guid.NewGuid().ToString(),
                            TimeSpan.FromSeconds(5)), 
                        1)),
                    TimeSpan.FromSeconds(5),
                    CancellationToken.None);

            await projector.StopAllInProgress(TimeSpan.FromSeconds(1));

            Response = await projectorTask;
            
            var projectorId = MurmurHash.StringHash(id).ToString();

            var coordinator = await Sys.ActorSelection($"/user/in-proc-projector-{projectionConfiguration.Name}")
                .ResolveOne(TimeSpan.FromSeconds(1));

            try
            {
                Projector = await
                    Sys.ActorSelection(coordinator, $"/{projectorId}")
                        .ResolveOne(TimeSpan.FromSeconds(1));
            }
            catch (Exception)
            {
                Projector = null;
            }
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}