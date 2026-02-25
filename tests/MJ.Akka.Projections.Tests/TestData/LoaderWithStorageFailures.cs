using System.Collections.Immutable;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Tests.TestData;

public class LoaderWithStorageFailures<TIdContext, TContext>(
    ILoadProjectionContext<TIdContext, TContext> innerLoader,
    IImmutableList<StorageFailures> failures) : ILoadProjectionContext<TIdContext, TContext>
    where TIdContext : IProjectionIdContext
    where TContext : IProjectionContext
{
    public Task<TContext> Load(TIdContext id, Func<TIdContext, TContext> getDefaultContext, CancellationToken cancellationToken = default)
    {
        foreach (var failure in failures)
            failure.MaybeFail(id);

        return innerLoader.Load(id, getDefaultContext, cancellationToken);
    }
}