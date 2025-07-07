using System.Collections.Immutable;
using JetBrains.Annotations;
using MJ.Akka.Projections.Documents;
using MJ.Akka.Projections.Storage.Messages;

namespace MJ.Akka.Projections.Storage.RavenDb;

[PublicAPI]
public class RavenDbProjectionContext<TDocument>(
    string id,
    TDocument? document,
    IImmutableDictionary<string, object> metadata)
    : ContextWithDocument<string, TDocument>(id, document)
    where TDocument : class
{
    private IImmutableDictionary<string, object> _metadata = metadata;

    public IEnumerable<IProjectionResult> SetMetadata(string key, object value)
    {
        _metadata = _metadata.SetItem(key, value);

        return [new StoreMetadata(Id, key, value)];
    }
    
    public object? GetMetadata(string key)
    {
        return _metadata.GetValueOrDefault(key);
    }
}