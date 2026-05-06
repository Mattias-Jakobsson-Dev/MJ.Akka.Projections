using MJ.Akka.Projections.Storage;
using MJ.Akka.Projections.Storage.InMemory;
using JetBrains.Annotations;

namespace MJ.Akka.Projections.Tests.Storage;

[PublicAPI]
public class InMemoryStashStorageTests : StashStorageTests
{
    protected override IProjectionStashStorage GetStorage() =>
        new InMemoryProjectionStashStorage();

    protected override IProjectionStashStorage GetStorageWithTimeout(TimeSpan timeout) =>
        new InMemoryProjectionStashStorage(inProcessTimeout: timeout);
}

