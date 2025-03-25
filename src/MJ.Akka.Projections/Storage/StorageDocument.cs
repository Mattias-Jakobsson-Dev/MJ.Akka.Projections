namespace MJ.Akka.Projections.Storage;

public abstract class StorageDocument<T>(object id)
    where T : StorageDocument<T>
{
    public object Id { get; } = id;

    public abstract Type DocumentType { get; }
    
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        
        return obj.GetType() == GetType() && Equals((StorageDocument<T>)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, DocumentType);
    }
    
    private bool Equals(StorageDocument<T> other)
    {
        return Id.Equals(other.Id) && DocumentType == other.DocumentType;
    }
}