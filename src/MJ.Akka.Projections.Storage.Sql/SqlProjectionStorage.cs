using System.Collections.Immutable;
using System.Data;
using System.Data.Common;

namespace MJ.Akka.Projections.Storage.Sql;

public class SqlProjectionStorage(DbConnection connection, IsolationLevel isolationLevel) : IProjectionStorage
{
    private readonly InProcessProjector<SqlStorageProjectorResult> _storageProjector = SqlStorageProjector.Setup();
    
    public async Task<StoreProjectionResponse> Store(
        StoreProjectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var (unhandledEvents, toStore) = _storageProjector.RunFor(
            request.Results.OfType<object>().ToImmutableList(),
            SqlStorageProjectorResult.Empty);

        if (toStore.CommandsToExecute.Any())
        {
            await using var transaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken);
            await using var batch = connection.CreateBatch();
            
            batch.Transaction = transaction;
            
            foreach (var command in toStore.CommandsToExecute)
            {
                var dbCommand = batch.CreateBatchCommand();
                
                dbCommand.CommandText = command.Command;

                foreach (var parameter in command.Parameters)
                {
                    var dbParameter = dbCommand.CreateParameter();
                    dbParameter.ParameterName = parameter.Name;
                    dbParameter.Value = parameter.Value ?? DBNull.Value;
                    dbCommand.Parameters.Add(dbParameter);
                }
            }
            
            await batch.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        
        return StoreProjectionResponse.From(unhandledEvents);
    }
}