using Akka.TestKit.Xunit2;
using MJ.Akka.Projections.Storage;
using Shouldly;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

public abstract class PositionStorageTests : global::Akka.TestKit.Xunit2.TestKit
{
    [Fact]
    public async Task LoadingNonExistingPosition()
    {
        var storage = GetStorage();

        var position = await storage.LoadLatestPosition(Guid.NewGuid().ToString());

        position.ShouldBeNull();
    }

    [Fact]
    public async Task LoadingRecentlyStoredPosition()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();

        await storage.StoreLatestPosition(projectionName, 10);

        var position = await storage.LoadLatestPosition(projectionName);

        position.ShouldBe(10);
    }

    [Fact]
    public async Task StoringNewHighPosition()
    {
        var storage = GetStorage();

        var position = await storage.StoreLatestPosition(Guid.NewGuid().ToString(), 10);

        position.ShouldBe(10);
    }

    [Fact]
    public async Task StoringLowPosition()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();

        await storage.StoreLatestPosition(projectionName, 10);

        var position = await storage.StoreLatestPosition(projectionName, 5);

        var loadedPosition = await storage.LoadLatestPosition(projectionName);

        position.ShouldBe(10);
        loadedPosition.ShouldBe(10);
    }

    [Fact]
    public async Task StoringPositionWithCancelledToken()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();
        var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            ConvertToAsync(() => storage.StoreLatestPosition(projectionName, 10, cancellation.Token)));
        
        var position = await storage.LoadLatestPosition(projectionName, CancellationToken.None);

        position.ShouldBeNull();
    }

    [Fact]
    public async Task LoadingPositionWithCancelledToken()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();
        var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            ConvertToAsync(() => storage.LoadLatestPosition(projectionName, cancellation.Token)));
    }

    [Fact]
    public async Task ResetNonExistingPosition()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();

        await storage.Reset(projectionName, 1);
        
        var position = await storage.LoadLatestPosition(projectionName);

        position.ShouldBe(1);
    }
    
    [Fact]
    public async Task ResetExistingPosition()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();

        await storage.StoreLatestPosition(projectionName, 2);

        await storage.Reset(projectionName, 1);
        
        var position = await storage.LoadLatestPosition(projectionName);

        position.ShouldBe(1);
    }
    
    [Fact]
    public async Task ResetExistingPositionToNull()
    {
        var storage = GetStorage();
        var projectionName = Guid.NewGuid().ToString();

        await storage.StoreLatestPosition(projectionName, 2);

        await storage.Reset(projectionName);
        
        var position = await storage.LoadLatestPosition(projectionName);

        position.ShouldBe(0);
    }

    private static async Task<T> ConvertToAsync<T>(Func<Task<T>> action)
    {
        return await action();
    }

    protected abstract IProjectionPositionStorage GetStorage();
}