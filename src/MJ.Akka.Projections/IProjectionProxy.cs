using JetBrains.Annotations;

namespace MJ.Akka.Projections;

[PublicAPI]
public interface IProjectionProxy
{
    IProjection Projection { get; }
        
    Task Stop();
    Task WaitForCompletion(TimeSpan? timeout = null);
}