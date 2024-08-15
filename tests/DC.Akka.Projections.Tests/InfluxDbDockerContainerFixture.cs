using System.Diagnostics;
using Docker.DotNet.Models;
using InfluxDB.Client;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Tests;

[PublicAPI]
public class InfluxDbDockerContainerFixture : DockerContainerFixture
{
    private static readonly Random Random = new();

    private int DbPort { get; } = Random.Next(7000, 9000);
    private string Token { get; } = Guid.NewGuid().ToString();
    private static string UserName => "test-user";
    private static string Password => "test-password";

    protected override string Image => "influxdb";
    protected override string Tag => "2.7";

    public string Organization { get; } = Guid.NewGuid().ToString();
    public string BucketName { get; } = Guid.NewGuid().ToString();

    public IInfluxDBClient CreateClient()
    {
        return new InfluxDBClient($"http://localhost:{DbPort}", Token);
    }

    protected override CreateContainerParameters Configure(CreateContainerParameters defaultConfig)
    {
        defaultConfig.ExposedPorts = new Dictionary<string, EmptyStruct>
        {
            { "8086/tcp", new EmptyStruct() }
        };

        defaultConfig.HostConfig = new HostConfig
        {
            PortBindings = new Dictionary<string, IList<PortBinding>>
            {
                {
                    "8086/tcp",
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
            "DOCKER_INFLUXDB_INIT_MODE=setup",
            $"DOCKER_INFLUXDB_INIT_USERNAME={UserName}",
            $"DOCKER_INFLUXDB_INIT_PASSWORD={Password}",
            $"DOCKER_INFLUXDB_INIT_ORG={Organization}",
            $"DOCKER_INFLUXDB_INIT_BUCKET={BucketName}",
            $"DOCKER_INFLUXDB_INIT_ADMIN_TOKEN={Token}"
        };

        return defaultConfig;
    }

    protected override async Task WaitForStart()
    {
        var client = CreateClient();
        
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(10))
        {
            var success = await client.PingAsync();
            
            if (success)
                break;

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        stopwatch.Stop();
    }
}