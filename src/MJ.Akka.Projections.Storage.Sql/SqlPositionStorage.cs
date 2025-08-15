using System.Data;
using System.Data.Common;

namespace MJ.Akka.Projections.Storage.Sql;

public class SqlPositionStorage(
    DbConnection connection,
    string tableName,
    IsolationLevel isolationLevel) : IProjectionPositionStorage
{
    public async Task<long?> LoadLatestPosition(string projectionName, CancellationToken cancellationToken = default)
    {
        await using var transaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken);

        await using var command = connection.CreateCommand();
        
        command.Transaction = transaction;
        
        command.CommandText = $"SELECT TOP 1 Position FROM {tableName} WHERE ProjectionName = @ProjectionName ORDER BY Position DESC";
        
        var parameter = command.CreateParameter();
        
        parameter.ParameterName = "@ProjectionName";
        parameter.Value = projectionName;
        command.Parameters.Add(parameter);
        
        long? position = null;

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                position = reader.IsDBNull(0) ? null : reader.GetInt64(0);
            }
        }
        
        await transaction.CommitAsync(cancellationToken);

        return position;
    }

    public async Task<long?> StoreLatestPosition(
        string projectionName,
        long? position,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken);

        await using var command = connection.CreateCommand();
        
        command.Transaction = transaction;
        command.CommandText = $"""
                               MERGE INTO {tableName} AS target
                               USING (SELECT @ProjectionName AS ProjectionName, @Position AS Position) AS source
                               ON target.ProjectionName = source.ProjectionName
                               WHEN MATCHED AND target.Position < source.Position THEN
                                   UPDATE SET Position = source.Position
                               WHEN NOT MATCHED THEN
                                   INSERT (ProjectionName, Position) VALUES (source.ProjectionName, source.Position)
                               OUTPUT inserted.Position;
                               """;
        
        var projectionNameParameter = command.CreateParameter();
        projectionNameParameter.ParameterName = "@ProjectionName";
        projectionNameParameter.Value = projectionName;
        command.Parameters.Add(projectionNameParameter);
        
        var positionParameter = command.CreateParameter();
        positionParameter.ParameterName = "@Position";
        positionParameter.Value = (object?)position ?? DBNull.Value;
        command.Parameters.Add(positionParameter);
        
        long? storedPosition = null;

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                storedPosition = reader.IsDBNull(0) ? null : reader.GetInt64(0);
            }
        }
        
        await transaction.CommitAsync(cancellationToken);

        return storedPosition;
    }
}