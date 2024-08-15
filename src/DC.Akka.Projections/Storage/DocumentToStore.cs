namespace DC.Akka.Projections.Storage;

public class DocumentToStore(object id, object document) 
    : StorageDocument<DocumentToStore>(id)
{
    public object Document { get; } = document;

    public override Type DocumentType => Document.GetType();
}