using System.Collections.Immutable;
using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Storage.Sql;

public record ExecuteSqlCommand(string Command, IImmutableList<ExecuteSqlCommand.Parameter> Parameters) 
    : IProjectionResult
{
    public record Parameter(string Name, object? Value);
}