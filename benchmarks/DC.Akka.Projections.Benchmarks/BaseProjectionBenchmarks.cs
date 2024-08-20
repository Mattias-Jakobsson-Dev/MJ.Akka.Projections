using System.Collections.Immutable;
using Akka.Actor;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using DC.Akka.Projections.Benchmarks.Columns;
using DC.Akka.Projections.Configuration;
using JetBrains.Annotations;

namespace DC.Akka.Projections.Benchmarks;

[Config(typeof(Config))]
public abstract class BaseProjectionBenchmarks
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddDiagnoser(MemoryDiagnoser.Default);
            AddLogger(ConsoleLogger.Default);
            AddColumn(new TotalEventsPerSecondColumn());
            AddColumn(new EventsPerDocumentPerSecondColumn());
        }
    }

    private ActorSystem ActorSystem { get; set; } = null!;
    private TestProjection _projection = null!;
    private IConfigureProjectionCoordinator _coordinator = null!;
    
    [IterationSetup]
    public virtual void Setup()
    {
        ActorSystem = ActorSystem.Create(
            "projections", 
            "akka.loglevel = ERROR");

        _projection = new TestProjection(Configuration.NumberOfEvents, Configuration.NumberOfDocuments);

        _coordinator = ActorSystem
            .Projections(conf => conf
                .WithProjection(_projection, Configure));
    }

    [PublicAPI]
    [ParamsSource(nameof(GetAvailableConfigurations))]
    public ProjectEventsConfiguration Configuration { get; set; } = null!;

    [Benchmark, EventsPerSecond(nameof(Configuration))]
    public async Task ProjectEvents()
    {
        var coordinator = await _coordinator.Start();

        await coordinator.Get(_projection.Name)!.WaitForCompletion();
    }

    protected abstract IHaveConfiguration<ProjectionInstanceConfiguration> Configure(
        IHaveConfiguration<ProjectionInstanceConfiguration> config);

    public static IImmutableList<ProjectEventsConfiguration> GetAvailableConfigurations()
    {
        return ImmutableList.Create(
            new ProjectEventsConfiguration(10_000, 1_000),
            new ProjectEventsConfiguration(10_000, 100),
            new ProjectEventsConfiguration(1_000, 1));
    }

    public record ProjectEventsConfiguration(int NumberOfEvents, int NumberOfDocuments)
    {
        public override string ToString()
        {
            return $"e: {NumberOfEvents}, d: {NumberOfDocuments}";
        }
    }
}