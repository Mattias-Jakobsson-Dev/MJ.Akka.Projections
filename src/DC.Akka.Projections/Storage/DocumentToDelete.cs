namespace DC.Akka.Projections.Storage;

public class DocumentToDelete(object id, Type documentType) 
    : StorageDocument<DocumentToDelete>(id)
{
    public override Type DocumentType => documentType;
}