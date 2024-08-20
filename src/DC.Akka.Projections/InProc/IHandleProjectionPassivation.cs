namespace DC.Akka.Projections.InProc;

public interface IHandleProjectionPassivation
{
    IHandler StartNew();
    
    public interface IHandler
    {
        void SetAndMaybeRemove(string id, Action<string> remove);
    }
}