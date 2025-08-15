using System.Collections.Immutable;

namespace MJ.Akka.Projections.Storage.Sql;

public record SqlStorageProjectorResult(IImmutableList<ExecuteSqlCommand> CommandsToExecute)
{
    public static SqlStorageProjectorResult Empty => new(ImmutableList<ExecuteSqlCommand>.Empty);
}