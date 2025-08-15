using System.Data;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.Sql;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

public class SqlServerPositionStorageTests(SqlServerFixture fixture) 
    : PositionStorageTests, IClassFixture<SqlServerFixture>
{
    protected override async Task<IProjectionPositionStorage> GetStorage()
    {
        var connection = fixture.CreateConnection();
        
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        
        command.CommandText = """
                              IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ProjectionPositions]') AND type in (N'U'))
                              BEGIN
                                  CREATE TABLE [dbo].[ProjectionPositions] (
                                      [ProjectionName] NVARCHAR(255) NOT NULL PRIMARY KEY,
                                      [Position] BIGINT NOT NULL
                                  );
                              END
                              """;
        
        await command.ExecuteNonQueryAsync();
        
        return new SqlPositionStorage(
            connection,
            "ProjectionPositions",
            IsolationLevel.ReadCommitted);
    }
}