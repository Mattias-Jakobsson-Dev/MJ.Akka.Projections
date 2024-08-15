using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using JetBrains.Annotations;
using Xunit;

namespace DC.Akka.Projections.Tests;

public abstract class DockerContainerFixture : IAsyncLifetime
{
    protected static readonly DockerClient Client;
    
    protected abstract string Image { get; }
    protected abstract string Tag { get; }

    [PublicAPI]
    protected string ContainerName { get; } = Guid.NewGuid().ToString();

    private string ImageFullName => $"{Image}:{Tag}";

    static DockerContainerFixture()
    {
        DockerClientConfiguration config;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            config = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            config = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine"));
        }
        else
        {
            throw new Exception("Unsupported OS");
        }

        Client = config.CreateClient();
    }

    public async Task InitializeAsync()
    {
        var images = await Client.Images.ListImagesAsync(new ImagesListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                {
                    "reference",
                    new Dictionary<string, bool>
                    {
                        { ImageFullName, true }
                    }
                }
            }
        });

        if (images.Count == 0)
        {
            await Client.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = Image, Tag = Tag }, null,
                new Progress<JSONMessage>(message =>
                {
                    Console.WriteLine(!string.IsNullOrEmpty(message.ErrorMessage)
                        ? message.ErrorMessage
                        : $"{message.ID} {message.Status} {message.ProgressMessage}");
                }));
        }
        
        await Client.Containers.CreateContainerAsync(Configure(new CreateContainerParameters
        {
            Image = ImageFullName,
            Name = ContainerName,
            Tty = true
        }));
        
        await Client.Containers.StartContainerAsync(
            ContainerName,
            new ContainerStartParameters());

        await WaitForStart();
    }

    public async Task DisposeAsync()
    {
        await Client.Containers.StopContainerAsync(ContainerName,
            new ContainerStopParameters { WaitBeforeKillSeconds = 0 });
        
        await Client.Containers.RemoveContainerAsync(ContainerName,
            new ContainerRemoveParameters { Force = true });
    }

    protected abstract CreateContainerParameters Configure(CreateContainerParameters defaultConfig);

    protected abstract Task WaitForStart();
}