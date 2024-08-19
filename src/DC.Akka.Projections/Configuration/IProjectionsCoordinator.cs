using System.Collections.Immutable;

namespace DC.Akka.Projections.Configuration;

public interface IProjectionsCoordinator
{
    Task<IImmutableList<IProjectionProxy>> GetAll();
    Task<IProjectionProxy?> Get(string projectionName);
}