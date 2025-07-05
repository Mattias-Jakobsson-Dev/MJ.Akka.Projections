using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;
using Akka.TestKit.Extensions;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.InfluxDb;
using FluentAssertions;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using JetBrains.Annotations;
using MJ.Akka.Projections.Storage.InMemory;
using Xunit;
using Source = Akka.Streams.Dsl.Source;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InfluxDbProjectionStorageTests(InfluxDbDockerContainerFixture fixture) 
    : ProjectionStorageTests<InfluxDbTimeSeriesId, InfluxDbTimeSeries, SetupInfluxDbStorage>, IClassFixture<InfluxDbDockerContainerFixture>
{
    private readonly IInfluxDBClient _client = fixture.CreateClient();
    private readonly string _measurementName = Guid.NewGuid().ToString();
    private readonly DateTime _now = DateTime.Now;

    public override async Task StoreAndDeleteSingleDocumentInSingleTransaction()
    {
        var storageSetup = GetStorage();

        var id = CreateRandomId();
        
        var projection = CreateProjection();
        
        var projectionStorage = storageSetup.CreateProjectionStorage();

        var addContext = CreateTestContext(id);
        var deleteContext = Delete(CreateTestContext(id));

        await projectionStorage
            .Store(new StoreProjectionRequest(
                addContext.PrepareForStorage().Items.AddRange(deleteContext.PrepareForStorage().Items)));
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var context = await loader.Load(id);

        context.Points.Should().BeEmpty();
        context.ToDelete.Should().BeEmpty();

        var items = await _client
            .GetQueryApi()
            .QueryAsync($"from(bucket:\"{fixture.BucketName}\") |> range(start: 0) |> filter(fn: (r) => r._measurement == \"{_measurementName}\")", fixture.Organization);

        items.Should().HaveCount(0);
    }

    public override async Task StoreAndDeleteSingleDocumentInTwoTransactions()
    {
        var storageSetup = GetStorage();

        var id = CreateRandomId();
        
        var projection = CreateProjection();
        
        var projectionStorage = storageSetup.CreateProjectionStorage();

        var addContext = CreateTestContext(id);
        var deleteContext = Delete(CreateTestContext(id));

        await projectionStorage
            .Store(new StoreProjectionRequest(addContext.PrepareForStorage().Items));
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var context = await loader.Load(id);
        
        context.Points.Should().BeEmpty();
        context.ToDelete.Should().BeEmpty();
        
        await VerifyContext(addContext, context);
        
        await projectionStorage
            .Store(new StoreProjectionRequest(deleteContext.PrepareForStorage().Items));
        
        context = await loader.Load(id);

        context.Points.Should().BeEmpty();
        context.ToDelete.Should().BeEmpty();

        var items = await _client
            .GetQueryApi()
            .QueryAsync($"from(bucket:\"{fixture.BucketName}\") |> range(start: 0) |> filter(fn: (r) => r._measurement == \"{_measurementName}\")", fixture.Organization);

        items.Should().HaveCount(0);
    }

    public override async Task DeleteNonExistingDocument()
    {
        var storageSetup = GetStorage();

        var id = CreateRandomId();
        
        var projection = CreateProjection();
        
        var projectionStorage = storageSetup.CreateProjectionStorage();
        
        var deleteContext = Delete(CreateTestContext(id));

        await projectionStorage
            .Store(new StoreProjectionRequest(deleteContext.PrepareForStorage().Items));
        
        var loader = projection.GetLoadProjectionContext(storageSetup);

        var context = await loader.Load(id);
        
        context.Points.Should().BeEmpty();
        context.ToDelete.Should().BeEmpty();

        var items = await _client
            .GetQueryApi()
            .QueryAsync($"from(bucket:\"{fixture.BucketName}\") |> range(start: 0) |> filter(fn: (r) => r._measurement == \"{_measurementName}\")", fixture.Organization);

        items.Should().HaveCount(0);
    }
    
    public override async Task WriteWithCancelledTask()
    {
        var cancellationTokenSource = new CancellationTokenSource();

        await cancellationTokenSource.CancelAsync();

        var storageSetup = GetStorage();

        var id = CreateRandomId();

        var original = CreateTestContext(id);

        var projectionStorage = storageSetup.CreateProjectionStorage();
        
        await projectionStorage
            .Store(new StoreProjectionRequest(original.PrepareForStorage().Items),
                cancellationTokenSource.Token)
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));
    }

    protected override SetupInfluxDbStorage GetStorage()
    {
        return new SetupInfluxDbStorage(_client, new InMemoryPositionStorage());
    }

    protected override InfluxDbTimeSeries CreateTestContext(InfluxDbTimeSeriesId id)
    {
        return new InfluxDbTimeSeries(
            id,
            ImmutableList.Create(PointData
                .Measurement(_measurementName)
                .Timestamp(_now, WritePrecision.S)
                .Field("test-field", 5d)
                .Tag("test-tag", "test")),
            ImmutableList<InfluxDbDeletePoint>.Empty);
    }

    protected override InfluxDbTimeSeries Delete(InfluxDbTimeSeries context)
    {
        return context
            .Delete(
                _now.AddSeconds(-1),
                _now.AddSeconds(1),
                $"_measurement=\"{_measurementName}\"");
    }

    protected override IProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeries, SetupInfluxDbStorage> CreateProjection()
    {
        return new TestProjection();
    }
    
    protected override async Task VerifyContext(InfluxDbTimeSeries original, InfluxDbTimeSeries loaded)
    {
        loaded.Points.Should().BeEmpty();
        loaded.ToDelete.Should().BeEmpty();

        var items = await _client
            .GetQueryApi()
            .QueryAsync($"from(bucket:\"{fixture.BucketName}\") |> range(start: 0) |> filter(fn: (r) => r._measurement == \"{_measurementName}\")", fixture.Organization);

        items.Should().HaveCount(1);

        items[0].Records.Should().HaveCount(1);
        
        items[0].Records[0].Values["_value"].Should().Be(5d);
    }

    protected override InfluxDbTimeSeriesId CreateRandomId()
    {
        return new InfluxDbTimeSeriesId(fixture.BucketName, fixture.Organization, Guid.NewGuid().ToString());
    }
    
    private class TestProjection : InfluxDbProjection
    {
        public override ISetupProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeries> Configure(
            ISetupProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeries> config)
        {
            return config;
        }

        public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
        {
            return Source.From(ImmutableList<EventWithPosition>.Empty);
        }
    }
}