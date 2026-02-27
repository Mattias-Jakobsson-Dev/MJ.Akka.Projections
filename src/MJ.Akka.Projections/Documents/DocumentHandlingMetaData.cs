using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Documents;

public record DocumentHandlingMetaData<TIdContext>(TIdContext Id, long? Position) 
    where TIdContext : IProjectionIdContext;