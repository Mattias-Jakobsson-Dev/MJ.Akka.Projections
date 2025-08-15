using System.Data.Common;
using System.Diagnostics;
using Docker.DotNet.Models;
using Microsoft.Data.SqlClient;

namespace MJ.Akka.Projections.Tests.Storage;

public class SqlServerFixture : DockerContainerFixture
{
    private static readonly Random Random = new();
    private static string Password => "Test-Password-1!";
    
    private int DbPort { get; } = Random.Next(10000, 12000);
    
    protected override string Image => "mcr.microsoft.com/mssql/server";
    protected override string Tag => "2022-latest";
    
    protected override CreateContainerParameters Configure(CreateContainerParameters defaultConfig)
    {
        defaultConfig.ExposedPorts = new Dictionary<string, EmptyStruct>
        {
            { "1433/tcp", new EmptyStruct() }
        };

        defaultConfig.HostConfig = new HostConfig
        {
            PortBindings = new Dictionary<string, IList<PortBinding>>
            {
                {
                    "1433/tcp",
                    new List<PortBinding>
                    {
                        new()
                        {
                            HostPort = $"{DbPort}"
                        }
                    }
                }
            }
        };

        defaultConfig.Env = new List<string>
        {
            "ACCEPT_EULA=Y",
            $"MSSQL_SA_PASSWORD={Password}"
        };

        return defaultConfig;
    }

    protected override async Task WaitForStart()
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(10))
        {
            await using var connection = CreateConnection("master");
            
            try
            {
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                
                command.CommandText = "CREATE DATABASE testdatabase;";
                
                await command.ExecuteNonQueryAsync();
                
                return;
            }
            catch (SqlException)
            {
                
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        stopwatch.Stop();
    }

    private SqlConnection CreateConnection(string database)
    {
        return new SqlConnection($"Server=localhost,{DbPort};Database={database};User Id=sa;Password={Password};TrustServerCertificate=True;");
    }

    public DbConnection CreateConnection() => CreateConnection("testdatabase");
}