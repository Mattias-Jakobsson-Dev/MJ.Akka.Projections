using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Storage;
using DC.Akka.Projections.Tests.TestData;

namespace DC.Akka.Projections.Tests;

public class When_projecting_two_events_to_two_simple_documents_with_batch_storage 
    : When_projecting_two_events_to_two_simple_documents_with_normal_storage
{
    public new class With_string_id : BaseTests<string>
    {
        protected override string FirstDocumentId { get; } = Guid.NewGuid().ToString();
        protected override string SecondDocumentId { get; } = Guid.NewGuid().ToString();
    }
    
    public new class With_int_id : BaseTests<int>
    {
        protected override int FirstDocumentId => 1;
        protected override int SecondDocumentId => 2;
    }

    public new abstract class BaseTests<TId>
        : When_projecting_two_events_to_two_simple_documents_with_normal_storage.BaseTests<TId> where TId : notnull
    {
        protected override IProjectionConfigurationSetup<TId, TestDocument<TId>> Configure(
            IProjectionConfigurationSetup<TId, TestDocument<TId>> config)
        {
            return base.Configure(config)
                .WithProjectionStorage(Storage.Batched(Sys));
        }
    }
}