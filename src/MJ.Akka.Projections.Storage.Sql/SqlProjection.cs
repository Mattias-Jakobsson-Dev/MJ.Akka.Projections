using JetBrains.Annotations;

namespace MJ.Akka.Projections.Storage.Sql;

[PublicAPI]
public abstract class SqlProjection<TId> : BaseProjection<TId, SqlProjectionContext, SetupSqlStorage>
    where TId : notnull
{
    public override ILoadProjectionContext<TId, SqlProjectionContext> GetLoadProjectionContext(
        SetupSqlStorage storageSetup)
    {
        return new LoadProjectionFromSql<TId>(
            storageSetup.GetConnection(),
            storageSetup.GetProjectionDataIsolationLevel(),
            TableName,
            IdColumn);
    }

    protected abstract string TableName { get; }
    protected virtual string IdColumn => "Id";
}