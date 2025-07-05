namespace MJ.Akka.Projections.Storage;

public interface IModifyStorage
{
    IStorageSetup Modify(IStorageSetup source);
}