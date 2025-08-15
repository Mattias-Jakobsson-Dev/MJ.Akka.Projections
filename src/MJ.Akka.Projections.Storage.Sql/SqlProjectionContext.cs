namespace MJ.Akka.Projections.Storage.Sql;

public class SqlProjectionContext(bool exists) : IProjectionContext
{
    public bool Exists()
    {
        return exists;
    }
}