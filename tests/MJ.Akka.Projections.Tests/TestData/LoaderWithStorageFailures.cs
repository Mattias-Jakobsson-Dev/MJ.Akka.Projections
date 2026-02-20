using System.Collections.Immutable;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Tests.TestData;

public class LoaderWithStorageFailures<TId, TContext>(
    ILoadProjectionContext<TId, TContext> innerLoader,
    IImmutableList<StorageFailures> failures) : ILoadProjectionContext<TId, TContext>
    where TId : notnull
    where TContext : IProjectionContext
{
    public Task<TContext> Load(TId id, Func<TId, TContext> getDefaultContext, CancellationToken cancellationToken = default)
    {
        foreach (var failure in failures)
            failure.MaybeFail(id);

        return innerLoader.Load(id, getDefaultContext, cancellationToken);
    }
}