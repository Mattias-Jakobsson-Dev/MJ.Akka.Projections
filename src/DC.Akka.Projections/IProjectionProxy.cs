using JetBrains.Annotations;

namespace DC.Akka.Projections;

[PublicAPI]
public interface IProjectionProxy
{
    void Stop();
    Task WaitForCompletion(TimeSpan? timeout = null);
}