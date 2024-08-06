using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Tests.TestData;

namespace DC.Akka.Projections.Tests;

public class When_projecting_two_events_to_simple_document_with_batch_storage 
    : When_projecting_two_events_to_simple_document_with_normal_storage
{
    public new class With_string_id : BaseTests<string>
    {
        protected override string DocumentId { get; } = Guid.NewGuid().ToString();
    }
    
    public new class With_int_id : BaseTests<int>
    {
        protected override int DocumentId => 1;
    }

    public new abstract class BaseTests<TId> 
        : When_projecting_two_events_to_simple_document_with_normal_storage.BaseTests<TId> where TId : notnull
    {
        protected override IProjectionConfigurationSetup<TId, TestDocument<TId>> Configure(
            IProjectionConfigurationSetup<TId, TestDocument<TId>> config)
        {
            return base.Configure(config)
                .WithProjectionStorage(Storage)
                .Batched((10, TimeSpan.FromMilliseconds(100)));
        }
    }
}