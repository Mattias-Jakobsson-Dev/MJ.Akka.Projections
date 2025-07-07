using System.Collections.Immutable;
using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Tests;

public class StorageFailures(
    Func<IProjectionResult, bool> checkStorageFailure,
    Func<object, bool> checkLoadFailure,
    Exception failWith)
{
    private readonly object _lock = new { };
    private bool _hasFailed;

    public void MaybeFail(IImmutableList<IProjectionResult> items)
    {
        if (_hasFailed)
            return;

        lock (_lock)
        {
            if (!items.Any(checkStorageFailure))
                return;

            _hasFailed = true;

            throw failWith;
        }
    }

    public void MaybeFail(object idToLoad)
    {
        if (_hasFailed)
            return;

        lock (_lock)
        {
            if (!checkLoadFailure(idToLoad))
                return;

            _hasFailed = true;

            throw failWith;
        }
    }
}