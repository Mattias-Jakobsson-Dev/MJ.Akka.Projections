using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Configuration;

public interface IProjectionStorageConfigurationSetup<TId, TDocument> 
    : IProjectionConfigurationSetup<TId, TDocument> where TId : notnull where TDocument : notnull
{
    IProjectionStorageConfigurationSetup<TId, TDocument> WithStorageSession(IStorageSession session);
}