using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public interface IProjectionProxy
{
    IProjection Projection { get; }

    Task WaitForCompletion(TimeSpan? timeout = null);
    
    Task Stop();
}