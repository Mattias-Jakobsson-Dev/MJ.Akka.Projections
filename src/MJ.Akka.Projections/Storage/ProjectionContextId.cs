using MJ.Akka.Projections.ProjectionIds;

namespace MJ.Akka.Projections.Storage;

public record ProjectionContextId(string ProjectionName, IProjectionIdContext ItemId);