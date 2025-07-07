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
using MJ.Akka.Projections.Setup;
using MJ.Akka.Projections.Storage.InMemory;
using MJ.Akka.Projections.Storage.Messages;
using Xunit;
using Source = Akka.Streams.Dsl.Source;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InfluxDbProjectionStorageTests(InfluxDbDockerContainerFixture fixture) 
    : ProjectionStorageTests<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, SetupInfluxDbStorage>, IClassFixture<InfluxDbDockerContainerFixture>
{
    private readonly IInfluxDBClient _client = fixture.CreateClient();
    private readonly string _measurementName = Guid.NewGuid().ToString();
    private readonly DateTime _now = DateTime.Now;

    public override async Task StoreAndDeleteSingleDocumentInSingleTransaction()
    {
        var storageSetup = GetStorage();

        var id = CreateRandomId();
        

        var projectionStorage = storageSetup.CreateProjectionStorage();

        var addContext = CreateInsertRequest(id);
        var deleteContext = CreateDeleteRequest(id);

        await projectionStorage.Store(new StoreProjectionRequest(
            addContext.Results.AddRange(deleteContext.Results)));
        

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

        var addContext = CreateInsertRequest(id);
        var deleteContext = CreateDeleteRequest(id);

        await projectionStorage.Store(addContext);
        
        var loader = projection.GetLoadProjectionContext(storageSetup);
        
        var context = await loader.Load(id);

        await VerifyContext(context);
        
        await projectionStorage.Store(deleteContext);

        var items = await _client
            .GetQueryApi()
            .QueryAsync($"from(bucket:\"{fixture.BucketName}\") |> range(start: 0) |> filter(fn: (r) => r._measurement == \"{_measurementName}\")", fixture.Organization);

        items.Should().HaveCount(0);
    }

    public override async Task DeleteNonExistingDocument()
    {
        var storageSetup = GetStorage();

        var id = CreateRandomId();

        var projectionStorage = storageSetup.CreateProjectionStorage();

        var deleteContext = CreateDeleteRequest(id);

        await projectionStorage.Store(deleteContext);

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

        var original = CreateInsertRequest(id);

        var projectionStorage = storageSetup.CreateProjectionStorage();
        
        await projectionStorage
            .Store(original,cancellationTokenSource.Token)
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));
    }

    protected override SetupInfluxDbStorage GetStorage()
    {
        return new SetupInfluxDbStorage(_client, new InMemoryPositionStorage());
    }

    protected override StoreProjectionRequest CreateInsertRequest(InfluxDbTimeSeriesId id)
    {
        return new StoreProjectionRequest(ImmutableList.Create<IProjectionResult>(
            new InfluxDbWritePoint(id, PointData
                .Measurement(_measurementName)
                .Timestamp(_now, WritePrecision.S)
                .Field("test-field", 5d)
                .Tag("test-tag", "test"))));
    }

    protected override StoreProjectionRequest CreateDeleteRequest(InfluxDbTimeSeriesId id)
    {
        return new StoreProjectionRequest(ImmutableList.Create<IProjectionResult>(new InfluxDbDeletePoint(
            id,
            _now.AddSeconds(-1),
            _now.AddSeconds(1),
            $"_measurement=\"{_measurementName}\"")));
    }

    protected override IProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext, SetupInfluxDbStorage> CreateProjection()
    {
        return new TestProjection();
    }

    protected override async Task VerifyContext(InfluxDbTimeSeriesContext loaded)
    {
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
        public override ISetupProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext> Configure(
            ISetupProjection<InfluxDbTimeSeriesId, InfluxDbTimeSeriesContext> config)
        {
            return config;
        }

        public override Source<EventWithPosition, NotUsed> StartSource(long? fromPosition)
        {
            return Source.From(ImmutableList<EventWithPosition>.Empty);
        }
    }
}