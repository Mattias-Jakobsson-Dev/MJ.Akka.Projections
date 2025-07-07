using MJ.Akka.Projections.Documents;

namespace MJ.Akka.Projections.Storage.InMemory;

public class InMemoryProjectionContext<TId, TDocument>(TId id, TDocument? document) 
    : ContextWithDocument<TId, TDocument>(id, document)
    where TId : notnull where TDocument : class;