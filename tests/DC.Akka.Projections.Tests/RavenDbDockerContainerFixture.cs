using System.Diagnostics;
using Docker.DotNet.Models;
using Raven.Client.Documents;

namespace DC.Akka.Projections.Tests;

public class RavenDbDockerContainerFixture : DockerContainerFixture
{
    private static readonly Random Random = new();
    
    private int DbPort { get; } = Random.Next(7000, 9000);
    protected override string Image => "ravendb/ravendb";
    protected override string Tag => "5.4-latest";
    
    protected override CreateContainerParameters Configure(CreateContainerParameters defaultConfig)
    {
        defaultConfig.ExposedPorts = new Dictionary<string, EmptyStruct>
        {
            { "8080/tcp", new EmptyStruct() }
        };

        defaultConfig.HostConfig = new HostConfig
        {
            PortBindings = new Dictionary<string, IList<PortBinding>>
            {
                {
                    "8080/tcp",
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
            "RAVEN_Setup_Mode=None",
            "RAVEN_License_Eula_Accepted=true",
            "RAVEN_Security_UnsecuredAccessAllowed=PrivateNetwork"
        };

        return defaultConfig;
    }

    protected override async Task WaitForStart()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var logStream = await Client.Containers.GetContainerLogsAsync(
            ContainerName,
            new ContainerLogsParameters
            {
                Follow = true,
                ShowStdout = true,
                ShowStderr = true
            });
#pragma warning restore CS0618 // Type or member is obsolete

        using (var reader = new StreamReader(logStream))
        {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < TimeSpan.FromSeconds(5) && await reader.ReadLineAsync() is { } line)
            {
                if (line.Contains("Running non-interactive.")) break;
            }

            stopwatch.Stop();
        }

        await logStream.DisposeAsync();
    }

    public IDocumentStore CreateDocumentStore(string database)
    {
        return new DocumentStore
        {
            Database = database,
            Urls =
            [
                $"http://localhost:{DbPort}"
            ]
        }.Initialize();
    }
}