using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public interface IProjectionProxy
{
    Task Stop();
    Task WaitForCompletion(TimeSpan? timeout = null);
}