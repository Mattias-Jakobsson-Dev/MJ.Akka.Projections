using Akka.TestKit.Extensions;
using Akka.TestKit.Xunit2;
using MJ.Akka.Projections.Storage;
using FluentAssertions;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

public abstract class PositionStorageTests : TestKit
{
    [Fact]
    public async Task LoadingNonExistingPosition()
    {
        var storage = await GetStorage();

        var position = await storage.LoadLatestPosition(Guid.NewGuid().ToString());

        position.Should().BeNull();
    }

    [Fact]
    public async Task LoadingRecentlyStoredPosition()
    {
        var storage = await GetStorage();
        var projectionName = Guid.NewGuid().ToString();

        await storage.StoreLatestPosition(projectionName, 10);

        var position = await storage.LoadLatestPosition(projectionName);

        position.Should().Be(10);
    }

    [Fact]
    public async Task StoringNewHighPosition()
    {
        var storage = await GetStorage();

        var position = await storage.StoreLatestPosition(Guid.NewGuid().ToString(), 10);

        position.Should().Be(10);
    }

    [Fact]
    public async Task StoringLowPosition()
    {
        var storage = await GetStorage();
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
        var storage = await GetStorage();
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
        var storage = await GetStorage();
        var projectionName = Guid.NewGuid().ToString();
        var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await ConvertToAsync(() => storage
                .LoadLatestPosition(projectionName, cancellation.Token))
            .ShouldThrowWithin<OperationCanceledException>(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ResetNonExistingPosition()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();

        await storage.Reset(projectionName, 1);
        
        var position = await storage.LoadLatestPosition(projectionName);

        position.Should().Be(1);
    }
    
    [Fact]
    public async Task ResetExistingPosition()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();

        await storage.StoreLatestPosition(projectionName, 2);

        await storage.Reset(projectionName, 1);
        
        var position = await storage.LoadLatestPosition(projectionName);

        position.Should().Be(1);
    }
    
    [Fact]
    public async Task ResetExistingPositionToNull()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();

        await storage.StoreLatestPosition(projectionName, 2);

        await storage.Reset(projectionName);
        
        var position = await storage.LoadLatestPosition(projectionName);

        position.Should().Be(0);
    }

    private static async Task<T> ConvertToAsync<T>(Func<Task<T>> action)
    {
        return await action();
    }

    protected abstract Task<IProjectionPositionStorage> GetStorage();
}