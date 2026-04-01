# AGENTS.md – MJ.Akka.Projections

## Project Overview
A .NET 8 / C# 12 library built on top of **Akka.NET** and **Akka.Streams** for building event-sourcing projections. Published as separate NuGet packages. The single test project covers all packages; the only example project is `EventStoreToRavenDbExample`.

## Repository Layout
```
src/
  MJ.Akka.Projections/              # Core library (Akka, Akka.Streams)
  MJ.Akka.Projections.Cluster.Sharding/   # Cluster-aware coordinators
  MJ.Akka.Projections.OpenTelemetry/
  MJ.Akka.Projections.Storage.RavenDb/    # RavenDB storage + RavenDbProjection base
  MJ.Akka.Projections.Storage.InfluxDb/
  MJ.Akka.Projections.Storage.Sql/        # (empty, in progress)
  MJ.Akka.Projections.TestKit/            # ProjectionTestKit base class
tests/MJ.Akka.Projections.Tests/          # All integration + unit tests (xUnit + Akka.TestKit)
examples/MJ.Akka.Projections.EventStoreToRavenDbExample/
benchmarks/MJ.Akka.Projections.Benchmarks/
```

## Developer Workflows
```bash
make build        # dotnet build
make test         # dotnet test (may need Docker for RavenDb/InfluxDb tests)
make package      # dotnet pack all src projects → ./.packages/
make publish      # package + nuget push (requires NUGET_KEY env var)
sudo make benchmark   # Release build benchmarks (Linux needs sudo)
make example      # Run the EventStore→RavenDb interactive example
```

## Architecture: Key Components

### Projection pipeline (top-down)
1. **`IProjection` / `BaseProjection<TIdContext,TContext,TStorageSetup>`** – user entry point. Implement `StartSource(long? fromPosition)` (Akka.Streams `Source<EventWithPosition,NotUsed>`), `Configure(ISetupProjection<…>)`, and `GetDefaultContext(id)`.
2. **`ProjectionsCoordinator`** (`ReceiveActor`) – owns the Akka.Streams stream. On `Start`, reads the last stored position, materialises the source, batches events via `IEventBatchingStrategy`, and fans out to `ProjectionSequencer`.
3. **`ProjectionSequencer`** (`ReceiveActor`) – per-id ordering; queues events for the same `IProjectionIdContext`, parallelises across different ids.
4. **`DocumentProjection`** (`ReceiveActor`) – per-document actor with states `NotLoaded → Loaded ↔ ProcessingEvents`. Loads context, runs handlers, stores result, advances position.

### Coordinator modes (`IConfigureProjectionCoordinator`)
- **`InProcessSingletonProjectionCoordinator`** – default; single in-process actor per projection.
- **`ClusterSingletonProjectionCoordinator`** – wraps coordinator in Akka Cluster Singleton.
- **`ShardedDaemonProjectionCoordinator`** – distributes projection shards via Akka Cluster ShardedDaemonProcess. Uses `StaticProjectionConfigurations` (a static registry keyed by `runnerId`) to pass config across serialisation boundaries.

### Storage abstractions (`IStorageSetup`)
- Provides `IProjectionStorage` (batch-write contexts) and `IProjectionPositionStorage` (read/write stream position).
- Built-in implementations: `SetupInMemoryStorage`, `SetupRavenDbStorage`.
- Wrap any setup with **batched writes**: `AddBatchedProjectionStorage` (Akka.Streams queue; strategies: `BatchSizeStorageBatchingStrategy`, `BufferWithinStorageBatchingStrategy`).

## Defining a Projection
Subclass a storage-specific base (or `BaseProjection` directly):

```csharp
// RavenDB – most common pattern
public class MyProjection : RavenDbProjection<MyDocument>
{
    public override ISetupProjectionHandlers<SimpleIdContext<string>, RavenDbProjectionContext<MyDocument, SimpleIdContext<string>>>
        Configure(ISetupProjection<SimpleIdContext<string>, RavenDbProjectionContext<MyDocument, SimpleIdContext<string>>> config)
    {
        return config
            .TransformUsing<SomeEvent>(e => ImmutableList.Create<object>(new OtherEvent(e.Id)))
            .On<OtherEvent>(e => e.Id)            // returns IProjectionIdContext (SimpleIdContext<string> implicit)
            .ModifyDocument((evnt, doc) => { /* mutate doc */ return doc; })
            .On<AnotherEvent>(e => e.Id)
            .HandleWith((evnt, ctx, pos, ct) => { /* arbitrary async */ return Task.CompletedTask; });
    }

    public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) { … }
}

// In-Memory – for tests / benchmarks
public class MyInMemoryProjection : InMemoryProjection<SimpleIdContext<string>, MyDocument> { … }
```

Key conventions:
- `On<TEvent>(getId)` maps an event type to a document id; returns `ISetupEventHandlerForProjection` for chaining `.HandleWith(…)` or `.ModifyDocument(…)`.
- Multiple `.HandleWith` calls on the same `.On<…>` chain execute in order.
- `.TransformUsing<T>` maps one event type to multiple before routing.
- `IProjectionIdContext.GetProjectorId()` returns an MD5 hex string used as actor name – implement `GetStringRepresentation()` when creating custom id contexts (or use `SimpleIdContext<T>`).

## Wiring Up the System
```csharp
var coordinator = await actorSystem
    .Projections(config => config
        .WithRestartSettings(RestartSettings.Create(…))
        .WithEventBatchingStrategy(BatchWithinEventBatchingStrategy.Default) // 100 items / 1 s
        .WithPositionStorageBatchingStrategy(BatchWithinEventPositionBatchingStrategy.Default)
        .WithProjection(new MyProjection(actorSystem))
        .WithModifiedStorage(new AddBatchedProjectionStorage.Modifier(…)),  // optional batching
        storageSetup)
    .Start();

var proxy = coordinator.Get("MyProjection");   // IProjectionProxy
await proxy.WaitForCompletion(timeout);        // blocks until source completes
await proxy.Stop();
```

## Testing Patterns
- **`ProjectionTestKit<TIdContext,TContext,TStorageSetup>`** (in `MJ.Akka.Projections.TestKit`) – xUnit `IAsyncLifetime`; override `GetProjectionToTest()`, `When()`, `Given()`, `Then()`. Automatically wires up, runs projection to completion, then calls `Then()`.
- **`BaseContinuousProjectionsTests`** – abstract base with shared facts (restart, failures, batching) parameterised by storage/id type. Concrete subclasses supply `CreateStorageSetup()`, `GetProjection()`, `Configure()`.
- Integration tests that touch RavenDB or InfluxDB spin up Docker containers via `DockerContainerFixture` / `RavenDbDockerContainerFixture`.
- Storage wrappers in tests: `TestStorageWrapper`, `TrackEventsStorageWrapper`, `RandomFailureStorageWrapper`, `TestFailureStorageWrapper` – all implement `IModifyStorage` and are injected via `.WithModifiedStorage(…)`.

## Batching Strategies (two separate concerns)
| Interface | Controls |
|---|---|
| `IEventBatchingStrategy` | How upstream events are grouped before entering the sequencer (e.g. `BatchWithinEventBatchingStrategy`, `NoEventBatchingStrategy`) |
| `IEventPositionBatchingStrategy` | How the persisted stream position is updated after batches complete (e.g. `BatchWithinEventPositionBatchingStrategy`, `NoBatchingPositionStrategy`) |
| `IStorageBatchingStrategy` | How document writes are batched in `BatchedProjectionStorage` (separate from event batching) |

## Public API Conventions
- All publicly intended types carry `[PublicAPI]` (JetBrains.Annotations, `<PrivateAssets>all</PrivateAssets>`).
- All projects target `net8.0`, `Nullable enable`, `LangVersion 12`.
- C# `record` types are used extensively for immutable messages and configuration (`ProjectionConfiguration`, `Messages.Acknowledge`, `Messages.Reject`, etc.).
- Version numbers are per-project in `<Version>` inside each `.csproj`; bump independently.

