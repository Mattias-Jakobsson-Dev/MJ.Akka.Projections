using System.Collections.Immutable;
using Akka.Actor;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using DC.Akka.Projections.Benchmarks.Columns;
using DC.Akka.Projections.OneTime;
using Hyperion.Internal;

namespace DC.Akka.Projections.Benchmarks;

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
    private TestProjection _projection = null!;
    private IOneTimeProjection<string, TestProjection.TestDocument> _coordinator = null!;
    
    [IterationSetup]
    public void Setup()
    {
        ActorSystem = ActorSystem.Create(
            "projections", 
            "akka.loglevel = ERROR");

        _projection = new TestProjection(Configuration.NumberOfEvents, Configuration.NumberOfDocuments);

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
    public BaseProjectionBenchmarks.ProjectEventsConfiguration Configuration { get; set; } = null!;
    
    public static IImmutableList<BaseProjectionBenchmarks.ProjectEventsConfiguration> GetAvailableConfigurations()
    {
        return ImmutableList.Create(
            new BaseProjectionBenchmarks.ProjectEventsConfiguration(10_000, 1_000),
            new BaseProjectionBenchmarks.ProjectEventsConfiguration(10_000, 100),
            new BaseProjectionBenchmarks.ProjectEventsConfiguration(1_000, 1));
    }
}