using System.Data;
using System.Data.Common;

namespace MJ.Akka.Projections.Storage.Sql;

public class LoadProjectionFromSql<TId>(
    DbConnection connection,
    IsolationLevel isolationLevel,
    string tableName,
    string idColumn) : ILoadProjectionContext<TId, SqlProjectionContext> 
    where TId : notnull
{
    public async Task<SqlProjectionContext> Load(TId id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken);
        
        await using var command = connection.CreateCommand();
        
        command.Transaction = transaction;
        command.CommandText = $"SELECT 1 FROM {tableName} WHERE {idColumn} = @Id";
        
        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "@Id";
        idParameter.Value = id;
        command.Parameters.Add(idParameter);
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var exists = await reader.ReadAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        
        return new SqlProjectionContext(exists);
    }
}