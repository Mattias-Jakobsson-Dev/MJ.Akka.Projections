using System.Collections.Immutable;
using Akka.Actor;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using JetBrains.Annotations;
using MJ.Akka.Projections.Benchmarks.Columns;
using MJ.Akka.Projections.OneTime;

namespace MJ.Akka.Projections.Benchmarks;

[Config(typeof(Config))]
public class OneTimeProjectionBenchmarks
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
    private InMemoryTestProjection _projection = null!;
    private IOneTimeProjection<string, InMemoryTestProjection.TestDocument> _coordinator = null!;
    
    [IterationSetup]
    public void Setup()
    {
        ActorSystem = ActorSystem.Create(
            "projections", 
            "akka.loglevel = ERROR");

        _projection = new InMemoryTestProjection(Configuration.NumberOfEvents, Configuration.NumberOfDocuments);

        _coordinator = ActorSystem
            .CreateOneTimeProjection(_projection);
    }
    
    [Benchmark, EventsPerSecond(nameof(Configuration))]
    public async Task ProjectEvents()
    {
        await _coordinator.Run();
    }

    [PublicAPI]
    [ParamsSource(nameof(GetAvailableConfigurations))]
    public ProjectEventsConfiguration Configuration { get; set; } = null!;
    
    public static IImmutableList<ProjectEventsConfiguration> GetAvailableConfigurations()
    {
        return ImmutableList.Create(
            new ProjectEventsConfiguration(10_000, 1_000),
            new ProjectEventsConfiguration(10_000, 100),
            new ProjectEventsConfiguration(1_000, 1));
    }
}