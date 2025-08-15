namespace MJ.Akka.Projections.Storage.Sql;

internal static class SqlStorageProjector
{
    public static InProcessProjector<SqlStorageProjectorResult> Setup()
    {
        return InProcessProjector<SqlStorageProjectorResult>.Setup(setup =>
        {
            setup.On<ExecuteSqlCommand>((evnt, result) =>
                new SqlStorageProjectorResult(result.CommandsToExecute.Add(evnt)));
        });
    }
}