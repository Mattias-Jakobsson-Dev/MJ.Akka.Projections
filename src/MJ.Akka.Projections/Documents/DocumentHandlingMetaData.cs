using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Documents;

[PublicAPI]
public record DocumentHandlingMetaData<TIdContext>(TIdContext Id, long? Position) 
    where TIdContext : IProjectionIdContext;