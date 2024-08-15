namespace DC.Akka.Projections.Configuration;

public interface IProjectionStoragePartSetup<out T> : IProjectionPartSetup<T> 
    where T : IProjectionPartSetup<T>
{
    IProjectionStoragePartSetup<T> Batched(int batchSize = 100, int parallelism = 5);
    
    T Config { get; }
}