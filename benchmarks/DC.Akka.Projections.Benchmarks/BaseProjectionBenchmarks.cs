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

    private ProjectionsCoordinator<string, TestProjection.TestDocument>.Proxy _projection = null!;
    
    [IterationSetup]
    public void Setup()
    {
        var sys = ActorSystem.Create(
            "projections", 
            "akka.loglevel = ERROR");

        _projection = sys
            .Projections()
            .WithProjection(
                new TestProjection(Configuration.NumberOfEvents, Configuration.NumberOfDocuments),
                Configure)
            .Result;
    }

    [PublicAPI]
    [ParamsSource(nameof(GetAvailableConfigurations))]
    public ProjectEventsConfiguration Configuration { get; set; } = null!;

    [Benchmark, EventsPerSecond(nameof(Configuration))]
    public async Task ProjectEvents()
    {
        await _projection.RunToCompletion();
    }

    protected abstract IProjectionConfigurationSetup<string, TestProjection.TestDocument> Configure(
        IProjectionConfigurationSetup<string, TestProjection.TestDocument> config);

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