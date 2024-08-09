namespace DC.Akka.Projections.Storage;

public class DocumentToDelete(object id, Type documentType) 
    : StorageDocument<DocumentToDelete>(id)
{
    protected override Type DocumentType => documentType;
}