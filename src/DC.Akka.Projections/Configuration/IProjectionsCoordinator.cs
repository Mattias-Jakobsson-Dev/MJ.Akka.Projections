using System.Collections.Immutable;

namespace DC.Akka.Projections.Configuration;

public interface IProjectionsCoordinator
{
    IImmutableList<IProjectionProxy> GetAll();
    IProjectionProxy? Get(string projectionName);
}