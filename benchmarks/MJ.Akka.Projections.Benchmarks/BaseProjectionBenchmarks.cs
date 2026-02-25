using System.Collections.Immutable;
using Akka.Actor;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using JetBrains.Annotations;
using MJ.Akka.Projections.Benchmarks.Columns;
using MJ.Akka.Projections.Configuration;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Benchmarks;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddDiagnoser(MemoryDiagnoser.Default);
        AddLogger(ConsoleLogger.Default);
        AddColumn(new TotalEventsPerSecondColumn());
        AddColumn(new EventsPerDocumentPerSecondColumn());
    }
}

public record ProjectEventsConfiguration(int NumberOfEvents, int NumberOfDocuments)
{
    public override string ToString()
    {
        return $"e: {NumberOfEvents}, d: {NumberOfDocuments}";
    }
}

public record BatchingStrategyConfiguration(string Name, IEventBatchingStrategy Strategy)
{
    public override string ToString()
    {
        return Name;
    }
}

[Config(typeof(BenchmarkConfig))]
public abstract class BaseProjectionBenchmarks<TIdContext, TContext, TStorageSetup>
    where TIdContext : IProjectionIdContext
    where TContext : IProjectionContext
    where TStorageSetup : IStorageSetup
{
    private ActorSystem ActorSystem { get; set; } = null!;
    private IProjection<TIdContext, TContext, TStorageSetup> _projection = null!;
    private IConfigureProjectionCoordinator _coordinator = null!;

    [IterationSetup]
    public virtual void Setup()
    {
        ActorSystem = ActorSystem.Create(
            "projections",
            "akka.loglevel = ERROR");

        _projection = CreateProjection(Configuration.NumberOfEvents, Configuration.NumberOfDocuments);

        _coordinator = ActorSystem
            .Projections(conf => ConfigureSystem(conf)
                    .WithEventBatchingStrategy(BatchingStrategy.Strategy)
                    .WithProjection(_projection, ConfigureProjection),
                GetStorageSetup());
    }

    [PublicAPI]
    [ParamsSource(nameof(GetAvailableConfigurations))]
    public ProjectEventsConfiguration Configuration { get; set; } = null!;

    [PublicAPI]
    [ParamsSource(nameof(GetAvailableBatchingStrategies))]
    public BatchingStrategyConfiguration BatchingStrategy { get; set; } = null!;

    [Benchmark, EventsPerSecond(nameof(Configuration))]
    public async Task ProjectEvents()
    {
        var coordinator = await _coordinator.Start();

        await coordinator.Get(_projection.Name)!.WaitForCompletion();
    }

    protected virtual IHaveConfiguration<ProjectionInstanceConfiguration> ConfigureProjection(
        IHaveConfiguration<ProjectionInstanceConfiguration> config) => config;

    protected virtual IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> ConfigureSystem(
        IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> config) => config;

    protected abstract TStorageSetup GetStorageSetup();

    protected abstract IProjection<TIdContext, TContext, TStorageSetup> CreateProjection(
        int numberOfEvents,
        int numberOfDocuments);

    public static IImmutableList<ProjectEventsConfiguration> GetAvailableConfigurations()
    {
        return ImmutableList.Create(
            new ProjectEventsConfiguration(10_000, 1_000),
            new ProjectEventsConfiguration(10_000, 100),
            new ProjectEventsConfiguration(1_000, 1));
    }

    public static IImmutableList<BatchingStrategyConfiguration> GetAvailableBatchingStrategies()
    {
        return ImmutableList.Create(
            new BatchingStrategyConfiguration("default", BatchEventBatchingStrategy.Default),
            new BatchingStrategyConfiguration("100 within 50ms",
                new BatchWithinEventBatchingStrategy(100, TimeSpan.FromMilliseconds(50))),
            new BatchingStrategyConfiguration("1 000 within 50ms",
                new BatchWithinEventBatchingStrategy(1_000, TimeSpan.FromMilliseconds(50))),
            new BatchingStrategyConfiguration("no batching", new NoEventBatchingStrategy(100)));
    }
}