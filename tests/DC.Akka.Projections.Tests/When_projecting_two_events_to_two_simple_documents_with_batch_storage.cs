using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.Tests.TestData;

namespace DC.Akka.Projections.Tests;

public class When_projecting_two_events_to_two_simple_documents_with_batch_storage 
    : When_projecting_two_events_to_two_simple_documents_with_normal_storage
{
    protected override IProjectionConfigurationSetup<string, TestDocument> Configure(
        IProjectionConfigurationSetup<string, TestDocument> config)
    {
        return base.Configure(config)
            .WithProjectionStorage(Storage)
            .Batched((10, TimeSpan.FromMilliseconds(100)));
    }
}