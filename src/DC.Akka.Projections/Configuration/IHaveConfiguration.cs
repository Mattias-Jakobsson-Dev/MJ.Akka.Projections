using Akka.Actor;

namespace DC.Akka.Projections.Configuration;

public interface IHaveConfiguration<T> where T : ProjectionConfig
{
    ActorSystem ActorSystem { get; }
    internal T Config { get; }

    internal IHaveConfiguration<T> WithModifiedConfig(Func<T, T> modify);
}