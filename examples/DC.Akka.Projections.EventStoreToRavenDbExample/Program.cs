// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Akka.Actor;
using DC.Akka.Projections.Configuration;
using DC.Akka.Projections.EventStoreToRavenDbExample;
using DC.Akka.Projections.Storage.RavenDb;
using Docker.DotNet;
using Docker.DotNet.Models;
using Raven.Client.Documents;
using Sharprompt;

var availableActions = new Dictionary<string, Func<Task>>
{
    ["start"] = Start,
    ["feed-data"] = FeedData,
    ["run-projection"] = RunProjection,
    ["stop"] = Stop
}.ToImmutableDictionary();

var action = availableActions[Prompt.Select("Select action", availableActions.Keys)];

await action();

return;

async Task Start()
{
    var dockerClient = CreateDockerClient();

    await StartDockerContainer(
        dockerClient,
        "eventstore/eventstore",
        "23.10.0-jammy",
        "dc-projections-example-event-store",
        config =>
        {
            config.ExposedPorts = new Dictionary<string, EmptyStruct>
            {
                { "2113/tcp", new EmptyStruct() }
            };

            config.Env = new List<string>
            {
                "EVENTSTORE_RUN_PROJECTIONS=All",
                "EVENTSTORE_MEM_DB=True",
                "EVENTSTORE_INSECURE=True",
                "EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP=True"
            };

            config.HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    {
                        "2113/tcp",
                        new List<PortBinding>
                        {
                            new()
                            {
                                HostPort = "2113"
                            }
                        }
                    }
                }
            };

            return config;
        });

    await StartDockerContainer(
        dockerClient,
        "ravendb/ravendb",
        "5.4-latest",
        "dc-projections-example-ravendb",
        config =>
        {
            config.ExposedPorts = new Dictionary<string, EmptyStruct>
            {
                { "8080/tcp", new EmptyStruct() }
            };

            config.HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    {
                        "8080/tcp",
                        new List<PortBinding>
                        {
                            new()
                            {
                                HostPort = "8080"
                            }
                        }
                    }
                }
            };

            config.Env = new List<string>
            {
                "RAVEN_Setup_Mode=None",
                "RAVEN_License_Eula_Accepted=true",
                "RAVEN_Security_UnsecuredAccessAllowed=PrivateNetwork"
            };

            return config;
        });
}

async Task FeedData()
{
    var numberOfActors = Prompt.Input<int>("Number of actors to use", 10000);
    var lowNumberOfEventsPerActor = Prompt.Input<int>("Lower bound of events", 5);
    var upperNumberOfEventsPerActor = Prompt.Input<int>("Upper bound of events", 10);

    var actorSystem = CreateActorSystem();

    var random = new Random();

    await Task.WhenAll(Enumerable
        .Range(0, numberOfActors)
        .Select(x =>
        {
            var numberOfEvents = random.Next(lowNumberOfEventsPerActor, upperNumberOfEventsPerActor);

            var actor = actorSystem.ActorOf(Props.Create(() => new EventFeeder($"feeder-{x}")), $"feeder-{x}");

            return actor.Ask<EventFeeder.Responses.FeedEventsResponse>(
                new EventFeeder.Commands.FeedEvents(numberOfEvents));
        }));
}

async Task RunProjection()
{
    var actorSystem = CreateActorSystem();

    var projection = new ExampleProjection(actorSystem);

    var documentStore = CreateDocumentStore();

    documentStore.EnsureDatabaseExists();

    var projectionsCoordinator = await actorSystem
        .Projections(
            conf => conf
                .WithProjection(projection)
                .WithRavenDbDocumentStorage(documentStore)
                .Batched()
                .WithRavenDbPositionStorage(documentStore))
        .Start();

    var proxy = projectionsCoordinator
        .Get(projection.Name);
    
    await proxy!
        .WaitForCompletion();
}

async Task Stop()
{
    var dockerClient = CreateDockerClient();

    await StopDockerContainer(dockerClient, "dc-projections-example-event-store");
    await StopDockerContainer(dockerClient, "dc-projections-example-ravendb");
}

ActorSystem CreateActorSystem()
{
    return ActorSystem.Create("dc-projections", """
                                                akka.persistence {
                                                	journal {
                                                	    plugin = akka.persistence.journal.eventstore
                                                	
                                                		eventstore {
                                                			class = "Akka.Persistence.EventStore.Journal.EventStoreJournal, Akka.Persistence.EventStore"
                                                
                                                			connection-string = "esdb://admin:changeit@localhost:2113?tls=false&tlsVerifyCert=false"

                                                			auto-initialize = true
                                                		}
                                                	}
                                                	
                                                	query.plugin = akka.persistence.query.journal.eventstore
                                                
                                                	query.journal.eventstore {
                                                		class = "Akka.Persistence.EventStore.Query.EventStoreReadJournalProvider, Akka.Persistence.EventStore"
                                                		write-plugin = akka.persistence.journal.eventstore
                                                	}
                                                }
                                                """);
}

async Task StartDockerContainer(
    DockerClient client,
    string image,
    string tag,
    string containerName,
    Func<CreateContainerParameters, CreateContainerParameters> configure)
{
    var imageFullName = $"{image}:{tag}";
    
    var images = await client.Images.ListImagesAsync(new ImagesListParameters
    {
        Filters = new Dictionary<string, IDictionary<string, bool>>
        {
            {
                "reference",
                new Dictionary<string, bool>
                {
                    { imageFullName, true }
                }
            }
        }
    });

    if (images.Count == 0)
    {
        await client.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = image, Tag = tag }, null,
            new Progress<JSONMessage>(message =>
            {
                Console.WriteLine(!string.IsNullOrEmpty(message.ErrorMessage)
                    ? message.ErrorMessage
                    : $"{message.ID} {message.Status} {message.ProgressMessage}");
            }));
    }
        
    await client.Containers.CreateContainerAsync(configure(new CreateContainerParameters
    {
        Image = imageFullName,
        Name = containerName,
        Tty = true
    }));
        
    await client.Containers.StartContainerAsync(
        containerName,
        new ContainerStartParameters());
}

async Task StopDockerContainer(DockerClient dockerClient, string containerName)
{
    await dockerClient.Containers.StopContainerAsync(containerName,
        new ContainerStopParameters { WaitBeforeKillSeconds = 0 });
    
    await dockerClient.Containers.RemoveContainerAsync(containerName,
        new ContainerRemoveParameters { Force = true });
}

IDocumentStore CreateDocumentStore()
{
    return new DocumentStore
    {
        Urls = ["http://localhost:8080"],
        Database = "projections"
    }.Initialize();
}

DockerClient CreateDockerClient()
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

    return config.CreateClient();
}