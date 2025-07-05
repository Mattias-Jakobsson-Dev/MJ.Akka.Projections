namespace MJ.Akka.Projections.Storage;

public interface IStorageSetup
{
    IProjectionStorage CreateProjectionStorage();
    IProjectionPositionStorage CreatePositionStorage();
}