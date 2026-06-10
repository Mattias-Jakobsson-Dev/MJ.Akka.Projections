# MJ.Akka.Projections

A .NET 8 library built on top of [Akka.NET](https://getakka.net/) and Akka.Streams for building **event-sourcing projections**. It reads a stream of events, routes them to per-document actor pipelines, and persists the resulting read-model documents — all with configurable batching, restarts, and storage backends.

## Packages

| Package | Description |
|---|---|
| `MJ.Akka.Projections` | Core library — projection pipeline, in-memory storage, event/position batching |
| `MJ.Akka.Projections.Storage.RavenDb` | RavenDB storage backend and `RavenDbProjection` base class |
| `MJ.Akka.Projections.Storage.InfluxDb` | InfluxDB storage backend |
| `MJ.Akka.Projections.Cluster.Sharding` | Cluster Singleton and Sharded Daemon coordinator modes |
| `MJ.Akka.Projections.TestKit` | `ProjectionTestKit` base class for xUnit tests |

## Requirements

- .NET 8
- An Akka.NET `ActorSystem`
- A storage backend (in-memory, RavenDB, …)

---

## Quick Start

### 1. Define a projection

Subclass the storage-specific base class, implement `Configure` to wire events to handlers, and implement `StartSource` to provide the upstream event stream.

```csharp
// RavenDB-backed projection
public class OrderProjection : RavenDbProjection<OrderDocument>
{
    private readonly ActorSystem _system;

    public OrderProjection(ActorSystem system) => _system = system;

    public override ISetupProjectionHandlers<SimpleIdContext<string>, RavenDbProjectionContext<OrderDocument, SimpleIdContext<string>>>
        Configure(ISetupProjection<SimpleIdContext<string>, RavenDbProjectionContext<OrderDocument, SimpleIdContext<string>>> config)
    {
        return config
            .On<OrderPlaced>().WithId(e => e.OrderId)
                .CreateDocument(e => new OrderDocument { Id = e.OrderId, Status = "Placed" })
            .On<OrderShipped>().WithId(e => e.OrderId)
                .ModifyDocument((e, doc) => doc! with { Status = "Shipped" });
    }

    public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
    {
        // Return your Akka.Streams Source of events here.
        // fromPosition is the last stored position, or null if starting fresh.
        return MyEventStore.ReadAll(fromPosition);
    }
}
```

For an **in-memory** projection (useful for tests and benchmarks):

```csharp
public class OrderInMemoryProjection : InMemoryProjection<SimpleIdContext<string>, OrderDocument>
{
    public override ISetupProjectionHandlers<...> Configure(...) { ... }
    public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition) { ... }
}
```

### 2. Start the projection system

Call the `Projections` extension method on your `ActorSystem`:

```csharp
IProjectionsCoordinator coordinator = await actorSystem
    .Projections(config => config
        .WithProjection(new OrderProjection(actorSystem)),
        storageSetup)   // e.g. new SetupRavenDbStorage(documentStore, new BulkInsertOptions())
    .Start();
```

### 3. Interact with a running projection

```csharp
IProjectionProxy proxy = coordinator.Get("OrderProjection")!;

// Wait until the source stream completes (one-time projections)
await proxy.WaitForCompletion(timeout: TimeSpan.FromSeconds(30));

// Stop the projection
await proxy.Stop();
```

---

## Defining Event Handlers

Inside `Configure`, chain calls to build the handler pipeline:

```csharp
config
    // Map an event to a document id
    .On<MyEvent>().WithId(e => e.Id)
        // Option A – mutate the document (RavenDb / InMemory helpers)
        .ModifyDocument((evnt, doc) => doc! with { Name = evnt.Name })
        // Option B – full async handler
        .WhenAny(h => h.HandleWith(async (evnt, context, position, ct) =>
        {
            context.Document!.Name = evnt.Name;
        }))

    // Transform one event into multiple before routing
    .On<OriginalEvent>().Transform(e =>
        ImmutableList.Create<object>(new DerivedEvent(e.Id), new AnotherEvent(e.Id)))

    // Filter: only run handlers when a condition is met
    .On<MyEvent>().WithId(e => e.Id)
        .When(filter => filter.WithEventFilter(e => e.IsRelevant), h => h.HandleWith(...))
```

Multiple `.HandleWith` calls on the same `.On<>` chain execute **in order**.

### Fetching external data with `WithData`

Sometimes routing or handling an event requires data that isn't in the event itself — for example, looking up a related document in a database. Use `.WithData` to fetch that data **once per event**; it is then carried alongside the event through `WithId` and into the handler, so no extra round-trips occur.

```csharp
config
    .On<OrderShipped>()
    .WithData(async evnt => await orderRepository.LoadAsync(evnt.OrderId))
    .WithId((evnt, order) => new SimpleIdContext<string>(order.CustomerId))  // data available here
    .WhenAny(h => h.HandleWith(async (evnt, ctx, order, position, ct) =>    // and here
    {
        ctx.ModifyDocument(doc =>
        {
            doc ??= new CustomerDocument { Id = order.CustomerId };
            doc.ShippedOrders = doc.ShippedOrders.Add(evnt.OrderId);
            return doc;
        });
    }))
```

You can also use the fetched data inside **`Transform`** to decide which derived events to produce:

```csharp
config
    .On<OrderPlaced>()
    .WithData(async evnt => await catalogService.GetProductAsync(evnt.ProductId))
    .Transform((evnt, product) => product.RequiresWarehouseUpdate
        ? ImmutableList.Create<object>(new WarehouseReservation(evnt.OrderId, product.Sku))
        : ImmutableList<object>.Empty)
```

**How it works:** `getData` is called exactly once per event, immediately after the transform/flatten step. The result is bundled into an internal envelope that travels through the routing and handler stages — no caching or repeated fetches.

### RavenDB-specific helpers

| Extension | Description |
|---|---|
| `.CreateDocument(e => new Doc())` | Creates the document when the event arrives |
| `.ModifyDocument((e, doc) => ...)` | Modifies an existing document |
| `.DeleteDocument()` | Marks the document for deletion |
| `.SetMetadata(key, value)` | Sets RavenDB document metadata |

---

## Configuration Options

All options are set via the fluent `Projections(config => ...)` builder:

```csharp
actorSystem.Projections(config => config
    // Restart the stream on failure
    .WithRestartSettings(RestartSettings.Create(
        minBackoff: TimeSpan.FromSeconds(3),
        maxBackoff: TimeSpan.FromSeconds(30),
        randomFactor: 0.2))

    // Group upstream events before entering the sequencer (default: 100 items / 1 s)
    .WithEventBatchingStrategy(new BatchWithinEventBatchingStrategy(maxItems: 200, timeout: TimeSpan.FromSeconds(2)))

    // Batch position writes (default: same as above)
    .WithPositionStorageBatchingStrategy(new BatchWithinEventPositionBatchingStrategy(200, TimeSpan.FromSeconds(2)))

    // Add batched document writes (reduces storage round-trips)
    .WithBatchedStorage(parallelism: 4)

    .WithProjection(new OrderProjection(actorSystem)),
    storageSetup);
```

### Event batching strategies

| Type | Behaviour |
|---|---|
| `BatchWithinEventBatchingStrategy` *(default)* | Groups up to N events or until a timeout elapses |
| `BatchEventBatchingStrategy` | Groups exactly N events |
| `NoEventBatchingStrategy` | No batching — each event is processed individually |

### Position batching strategies

| Type | Behaviour |
|---|---|
| `BatchWithinEventPositionBatchingStrategy` *(default)* | Persists the position up to N times or until a timeout |
| `NoBatchingPositionStrategy` | Persists after every event |

### Storage batching strategies

| Type | Behaviour |
|---|---|
| `BufferWithinStorageBatchingStrategy` | Batches writes by count + time window |
| `BatchSizeStorageBatchingStrategy` | Batches writes by count only |

---

## Storage Backends

### In-Memory

No external dependencies — stores documents in a `ConcurrentDictionary`. Ideal for tests.

```csharp
var storageSetup = new SetupInMemoryStorage();
```

### RavenDB

```csharp
var storageSetup = new SetupRavenDbStorage(documentStore, new BulkInsertOptions());
```

Projection position is stored in a dedicated `ProjectionPosition` document in RavenDB.

---

## Coordinator Modes

Coordinator modes control how and where the projection actor runs.

### In-Process Singleton *(default)*

A single coordinator actor per projection, running inside the current process.

```csharp
// This is the default — no extra configuration needed.
```

### Cluster Singleton

Runs the coordinator as an Akka Cluster Singleton so only one node in the cluster drives the projection at a time. Requires `MJ.Akka.Projections.Cluster.Sharding`.

```csharp
config.WithCoordinator(new ClusterSingletonProjectionCoordinator.Setup(
    actorSystem,
    ClusterSingletonManagerSettings.Create(actorSystem)))
```

### Sharded Daemon

Distributes projections across the cluster using Akka ShardedDaemonProcess. Each projection is assigned to a node; if that node leaves, another picks it up.

```csharp
config.WithCoordinator(new ShardedDaemonProjectionCoordinator.Setup(
    actorSystem,
    name: "projections",
    ShardedDaemonProcessSettings.Create(actorSystem)))
```

---

## Projector Passivation

The framework spawns one actor per unique document id. To cap memory usage, configure passivation:

```csharp
// Default: keep the 1,000 most-recently-active projectors
config.WithProjectionFactory(
    new KeepTrackOfProjectorsInProc(actorSystem, new MaxNumberOfProjectorsPassivation(maxNumberOfProjectors: 500)))

// Or keep all projectors alive indefinitely
config.WithProjectionFactory(
    new KeepTrackOfProjectorsInProc(actorSystem, new KeepAllProjectors()))
```

---

## Architecture Overview

```
ActorSystem.Projections(...)
    └── IConfigureProjectionCoordinator
            └── ProjectionsCoordinator (ReceiveActor)
                    Reads last position → materialises Source<EventWithPosition>
                    Groups events via IEventBatchingStrategy
                    └── ProjectionSequencer (ReceiveActor)
                            Transform events → flatten
                            PrepareEvent (fetches WithData payload, wraps in envelope)
                            Route by id, serialise per id, parallelise across ids
                            └── DocumentProjection (ReceiveActor, one per id)
                                    Loads context from storage
                                    Runs registered handlers (unwraps envelope if present)
                                    Saves updated context
                                    Advances stream position via IEventPositionBatchingStrategy
```

---

## OpenTelemetry

The `MJ.Akka.Projections.OpenTelemetry` package adds zero-code-change observability to any projection system. It uses the standard .NET `ActivitySource` / `Meter` primitives so you can export to any OTEL-compatible backend (Prometheus, Jaeger, OTLP, …).

### Installation

```
dotnet add package MJ.Akka.Projections.OpenTelemetry
```

### Wiring up

#### 1. Enable instrumentation on the projection builder

Call `.WithOpenTelemetry()` when constructing the projection system. This wraps the position storage with latency-recording decorators and activates all metric / trace emission:

```csharp
IProjectionsCoordinator coordinator = await actorSystem
    .Projections(config => config
        .WithProjection(new OrderProjection(actorSystem))
        .WithOpenTelemetry(),   // ← add this
        storageSetup)
    .Start();
```

#### 2. Register with the OpenTelemetry SDK

Use the convenience extension methods when configuring your OTEL pipeline:

```csharp
// Tracing
services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddMJAkkaProjectionsInstrumentation()   // registers ActivitySource
        .AddOtlpExporter())

// Metrics
    .WithMetrics(metrics => metrics
        .AddMJAkkaProjectionsInstrumentation()   // registers Meter
        .AddPrometheusExporter());
```

Or register the names manually if you prefer:

```csharp
tracerProviderBuilder.AddSource(ProjectionInstrumentation.ActivitySourceName);
meterProviderBuilder.AddMeter(ProjectionInstrumentation.MeterName);
```

Both `ActivitySourceName` and `MeterName` are `"MJ.Akka.Projections"`.

---

### Emitted telemetry

#### Traces

| Span name | Description |
|---|---|
| `projection.handle_event` | One span per per-document event-handling cycle. Tagged with `projection.name` and the document id. |

#### Metrics

All metrics are tagged with `projection.name` where applicable.

| Metric name | Type | Unit | Description |
|---|---|---|---|
| `projection.events.processed` | Counter | `{events}` | Total events successfully processed. |
| `projection.events.failed` | Counter | `{events}` | Total events that failed processing. |
| `projection.restarts` | Counter | `{restarts}` | Number of times the projection source has been restarted. |
| `projection.position` | ObservableGauge | `{events}` | Latest committed event position for each projection. |
| `projection.event_handling.duration` | Histogram | `ms` | Duration of a per-document event-handling cycle. |
| `projection.position_storage.load.duration` | Histogram | `ms` | Duration of loading the latest position from storage. |
| `projection.position_storage.store.duration` | Histogram | `ms` | Duration of storing the latest position to storage. |
| `projection.withdata.fetch.duration` | Histogram | `ms` | Duration of each `WithData` `getData` call (tagged with `event.type`). |
| `projection.withdata.fetch.failures` | Counter | `{failures}` | Number of failures thrown by a `WithData` `getData` call (tagged with `event.type`). |
| `projection.groups.active` | UpDownCounter | `{groups}` | In-flight projection groups in the sequencer. |
| `projection.tasks.active` | UpDownCounter | `{tasks}` | In-flight projection tasks in the sequencer. |
| `projection.queue.depth` | UpDownCounter | `{batches}` | Event batches waiting in per-id sequencer queues. |

---

## Custom Storage

Implement `IStorageSetup` to plug in any storage backend:

```csharp
public class MyStorageSetup : IStorageSetup
{
    public IProjectionStorage CreateProjectionStorage() => new MyProjectionStorage();
    public IProjectionPositionStorage CreatePositionStorage() => new MyPositionStorage();
}
```

`IProjectionStorage` persists the read-model documents; `IProjectionPositionStorage` persists the last processed stream position so the projection can resume after a restart.

---

## Testing

Use `ProjectionTestKit<TIdContext, TContext, TStorageSetup>` from `MJ.Akka.Projections.TestKit` as the base class for xUnit tests:

```csharp
public class OrderProjectionTests
    : ProjectionTestKit<SimpleIdContext<string>, InMemoryProjectionContext<SimpleIdContext<string>, OrderDocument>, SetupInMemoryStorage>
{
    protected override IProjection<...> GetProjectionToTest() => new OrderInMemoryProjection();

    protected override Task Given() =>
        // Publish events into the test source
        PublishEvent(new OrderPlaced("order-1"));

    protected override Task Then()
    {
        var doc = GetDocument<OrderDocument>("order-1");
        Assert.Equal("Placed", doc.Status);
        return Task.CompletedTask;
    }
}
```

The kit wires up the projection, runs it to completion, and then calls `Then()`.
