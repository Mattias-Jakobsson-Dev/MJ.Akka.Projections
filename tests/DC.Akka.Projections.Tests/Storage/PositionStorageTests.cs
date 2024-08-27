using Akka.TestKit.Extensions;
using Akka.TestKit.Xunit2;
using DC.Akka.Projections.Storage;
using FluentAssertions;
using Xunit;

namespace DC.Akka.Projections.Tests.Storage;

public abstract class PositionStorageTests : TestKit
{
    [Fact]
    public async Task LoadingNonExistingPosition()
    {
        var storage = GetStorage();

        var position = await storage.LoadLatestPosition(Guid.NewGuid().ToString());

        position.Should().BeNull();
    }

    [Fact]
    public async Task LoadingRecentlyStoredPosition()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();

        await storage.StoreLatestPosition(projectionName, 10);

        var position = await storage.LoadLatestPosition(projectionName);

        position.Should().Be(10);
    }

    [Fact]
    public async Task StoringNewHighPosition()
    {
        var storage = GetStorage();

        var position = await storage.StoreLatestPosition(Guid.NewGuid().ToString(), 10);

        position.Should().Be(10);
    }

    [Fact]
    public async Task StoringLowPosition()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();

        await storage.StoreLatestPosition(projectionName, 10);

        var position = await storage.StoreLatestPosition(projectionName, 5);

        var loadedPosition = await storage.LoadLatestPosition(projectionName);

        position.Should().Be(10);
        loadedPosition.Should().Be(10);
    }

    [Fact]
    public async Task StoringPositionWithCancelledToken()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();
        var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await ConvertToAsync(() => storage
                .StoreLatestPosition(projectionName, 10, cancellation.Token))
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));
        
        var position = await storage.LoadLatestPosition(projectionName, CancellationToken.None);

        position.Should().BeNull();
    }

    [Fact]
    public async Task LoadingPositionWithCancelledToken()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();
        var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await ConvertToAsync(() => storage
                .LoadLatestPosition(projectionName, cancellation.Token))
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));
    }

    private static async Task<T> ConvertToAsync<T>(Func<Task<T>> action)
    {
        return await action();
    }

    protected abstract IProjectionPositionStorage GetStorage();
}