namespace MJ.Akka.Projections;

public interface IProjectionContext
{
    bool Exists();

    IProjectionContext MergeWith(IProjectionContext later);

    IProjectionContext Freeze();
}