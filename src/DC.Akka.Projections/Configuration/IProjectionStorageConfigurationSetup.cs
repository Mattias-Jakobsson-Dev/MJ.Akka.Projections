namespace DC.Akka.Projections.Configuration;

public interface IProjectionStorageConfigurationSetup<TDocument> : IProjectionConfigurationSetup<TDocument>
{
    IProjectionStorageConfigurationSetup<TDocument> Batched(
        (int Number, TimeSpan Timeout)? batching = null,
        int parallelism = 10);
}