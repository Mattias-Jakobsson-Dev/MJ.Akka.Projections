using System.Collections.Immutable;

namespace MJ.Akka.Projections.Storage;

public class InvalidContextStorageException(IImmutableList<ProjectionContextId> invalidIds) 
    : Exception($"The following projection context IDs are invalid: {string.Join(", ", invalidIds)}");