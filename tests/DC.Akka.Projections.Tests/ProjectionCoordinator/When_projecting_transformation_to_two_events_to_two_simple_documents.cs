using System.Collections.Immutable;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Tests.TestData;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Tests.ProjectionCoordinator;

public class When_projecting_transformation_to_two_events_to_two_simple_documents
{
    public class With_normal_storage
    {
        [PublicAPI]
        public class With_string_id(NormalStorageFixture<string> fixture)
            : BaseTests<string, NormalStorageFixture<string>>(fixture);

        [PublicAPI]
        public class With_int_id(NormalStorageFixture<int> fixture)
            : BaseTests<int, NormalStorageFixture<int>>(fixture);
    }

    public class With_batched_storage
    {
        [PublicAPI]
        public class With_string_id(BatchedStorageFixture<string> fixture)
            : BaseTests<string, BatchedStorageFixture<string>>(fixture);

        [PublicAPI]
        public class With_int_id(BatchedStorageFixture<int> fixture)
            : BaseTests<int, BatchedStorageFixture<int>>(fixture);
    }

    public abstract class BaseTests<TId, TFixture>(TFixture fixture)
        : When_projecting_two_events_to_two_simple_documents.BaseTests<TId, TFixture>(fixture)
        where TFixture : When_projecting_two_events_to_two_simple_documents.BaseFixture<TId> where TId : notnull
    {
        protected override int ExpectedPosition => 1;
    }

    [PublicAPI]
    public class NormalStorageFixture<TId> : When_projecting_two_events_to_two_simple_documents.NormalStorageFixture<TId>
        where TId : notnull
    {
        protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration> setup)
        {
            return setup
                .WithTestProjection<TId>(
                    ImmutableList.Create<object>(new Events<TId>.TransformToMultipleEvents(
                        ImmutableList.Create<Events<TId>.IEvent>(
                            new Events<TId>.FirstEvent(FirstDocumentId, FirstEventId),
                            new Events<TId>.SecondEvent(SecondDocumentId, SecondEventId)))),
                    ConfigureProjection);
        }
    }

    [PublicAPI]
    public class BatchedStorageFixture<TId> : When_projecting_two_events_to_two_simple_documents.BatchedStorageFixture<TId>
        where TId : notnull
    {
        protected override IHaveConfiguration<ProjectionSystemConfiguration> Configure(
            IHaveConfiguration<ProjectionSystemConfiguration> setup)
        {
            return setup
                .WithTestProjection<TId>(
                    ImmutableList.Create<object>(new Events<TId>.TransformToMultipleEvents(
                        ImmutableList.Create<Events<TId>.IEvent>(
                            new Events<TId>.FirstEvent(FirstDocumentId, FirstEventId),
                            new Events<TId>.SecondEvent(SecondDocumentId, SecondEventId)))),
                    ConfigureProjection);
        }
    }
}