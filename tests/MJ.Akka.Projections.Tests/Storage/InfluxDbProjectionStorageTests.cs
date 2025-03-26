using System.Collections.Immutable;
using Akka.TestKit.Extensions;
using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.InfluxDb;
using FluentAssertions;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxDB.Client.Writes;
using JetBrains.Annotations;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InfluxDbProjectionStorageTests(InfluxDbDockerContainerFixture fixture) 
    : ProjectionStorageTests<InfluxDbTimeSeriesId, InfluxTimeSeries>, IClassFixture<InfluxDbDockerContainerFixture>
{
    private readonly IInfluxDBClient _client = fixture.CreateClient();
    private readonly string _measurementName = Guid.NewGuid().ToString();
    private readonly DateTime _now = DateTime.Now;

    public override async Task StoreAndDeleteSingleDocumentInSingleTransaction()
    {
        var storage = GetStorage();

        var id = CreateRandomId();

        await storage
            .Store(
                ImmutableList.Create(new DocumentToStore(
                    id,
                    CreateTestDocument(id) with
                    {
                        ToDelete = ImmutableList.Create(new InfluxTimeSeries.DeletePoint(
                            _now.AddSeconds(-1),
                            _now.AddSeconds(1),
                            $"_measurement=\"{_measurementName}\""))
                    })),
                ImmutableList<DocumentToDelete>.Empty);
        
        var document = await storage.LoadDocument<InfluxTimeSeries>(id);

        document.Should().NotBeNull();
        
        document!.Points.Should().BeEmpty();
        document.ToDelete.Should().BeEmpty();

        var items = await _client
            .GetQueryApi()
            .QueryAsync($"from(bucket:\"{fixture.BucketName}\") |> range(start: 0) |> filter(fn: (r) => r._measurement == \"{_measurementName}\")", fixture.Organization);

        items.Should().HaveCount(0);
    }

    public override async Task StoreAndDeleteSingleDocumentInTwoTransactions()
    {
        var storage = GetStorage();

        var id = CreateRandomId();

        var testDocument = CreateTestDocument(id);

        await storage
            .Store(
                ImmutableList.Create(new DocumentToStore(id, testDocument)),
                ImmutableList<DocumentToDelete>.Empty);
        
        var document = await storage.LoadDocument<InfluxTimeSeries>(id);

        document.Should().NotBeNull();
        
        await VerifyDocument(testDocument, document!);

        await storage
            .Store(
                ImmutableList.Create(new DocumentToStore(
                    id,
                    new InfluxTimeSeries(
                        ImmutableList<PointData>.Empty, 
                        ImmutableList.Create(new InfluxTimeSeries.DeletePoint(
                            _now.AddSeconds(-1),
                            _now.AddSeconds(1),
                            $"_measurement=\"{_measurementName}\""))))),
                ImmutableList<DocumentToDelete>.Empty);
        
        document = await storage.LoadDocument<InfluxTimeSeries>(id);

        document.Should().NotBeNull();
        
        document!.Points.Should().BeEmpty();
        document.ToDelete.Should().BeEmpty();

        var items = await _client
            .GetQueryApi()
            .QueryAsync($"from(bucket:\"{fixture.BucketName}\") |> range(start: 0) |> filter(fn: (r) => r._measurement == \"{_measurementName}\")", fixture.Organization);

        items.Should().HaveCount(0);
    }

    public override async Task DeleteNonExistingDocument()
    {
        var storage = GetStorage();

        var id = CreateRandomId();

        await storage
            .Store(
                ImmutableList.Create(new DocumentToStore(
                    id,
                    new InfluxTimeSeries(
                        ImmutableList<PointData>.Empty, 
                        ImmutableList.Create(new InfluxTimeSeries.DeletePoint(
                            _now.AddSeconds(-1),
                            _now.AddSeconds(1),
                            $"_measurement=\"{_measurementName}\""))))),
                ImmutableList<DocumentToDelete>.Empty);
        
        var document = await storage.LoadDocument<InfluxTimeSeries>(id);

        document.Should().NotBeNull();
        
        document!.Points.Should().BeEmpty();
        document.ToDelete.Should().BeEmpty();

        var items = await _client
            .GetQueryApi()
            .QueryAsync($"from(bucket:\"{fixture.BucketName}\") |> range(start: 0) |> filter(fn: (r) => r._measurement == \"{_measurementName}\")", fixture.Organization);

        items.Should().HaveCount(0);
    }
    
    [Fact]
    public override async Task WriteWithCancelledTask()
    {
        var storage = GetStorage();

        var id = CreateRandomId();
        
        var cancellationTokenSource = new CancellationTokenSource();

        await cancellationTokenSource.CancelAsync();

        await storage
            .Store(
                ImmutableList.Create(new DocumentToStore(
                    id,
                    new InfluxTimeSeries(
                        ImmutableList<PointData>.Empty, 
                        ImmutableList.Create(new InfluxTimeSeries.DeletePoint(
                            _now.AddSeconds(-1),
                            _now.AddSeconds(1),
                            $"_measurement=\"{_measurementName}\""))))),
                ImmutableList<DocumentToDelete>.Empty, 
                cancellationTokenSource.Token)
            .ShouldThrowWithin<HttpException>(TimeSpan.FromSeconds(1));
    }

    protected override IProjectionStorage GetStorage()
    {
        return new InfluxDbProjectionStorage(_client);
    }

    protected override InfluxTimeSeries CreateTestDocument(InfluxDbTimeSeriesId id)
    {
        return new InfluxTimeSeries(
            ImmutableList.Create(PointData
                .Measurement(_measurementName)
                .Timestamp(_now, WritePrecision.S)
                .Field("test-field", 5d)
                .Tag("test-tag", "test")),
            ImmutableList<InfluxTimeSeries.DeletePoint>.Empty);
    }

    protected override async Task VerifyDocument(InfluxTimeSeries original, InfluxTimeSeries loaded)
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
}