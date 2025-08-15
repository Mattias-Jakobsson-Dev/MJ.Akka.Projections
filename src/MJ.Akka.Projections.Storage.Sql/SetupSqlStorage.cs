using System.Data;
using System.Data.Common;

namespace MJ.Akka.Projections.Storage.Sql;

public class SetupSqlStorage(
    DbConnection connection,
    IsolationLevel projectionDataIsolationLevel,
    string positionStorageTableName,
    IsolationLevel positionStorageIsolationLevel) : IStorageSetup
{
    public IProjectionStorage CreateProjectionStorage()
    {
        return new SqlProjectionStorage(connection, projectionDataIsolationLevel);
    }

    public IProjectionPositionStorage CreatePositionStorage()
    {
        return new SqlPositionStorage(connection, positionStorageTableName, positionStorageIsolationLevel);
    }
    
    public DbConnection GetConnection()
    {
        return connection;
    }
    
    public IsolationLevel GetProjectionDataIsolationLevel()
    {
        return projectionDataIsolationLevel;
    }
}