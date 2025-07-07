using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Documents;

public static class DocumentResults
{
    public record DocumentCreated(object Id, object Document) : IProjectionResult;
    
    public record DocumentDeleted(object Id) : IProjectionResult;
    
    public record DocumentModified(object Id, object Document) : IProjectionResult;
}