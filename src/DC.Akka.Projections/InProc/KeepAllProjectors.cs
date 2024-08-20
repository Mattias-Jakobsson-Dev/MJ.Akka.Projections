namespace DC.Akka.Projections.InProc;

public class KeepAllProjectors : IHandleProjectionPassivation
{
    public IHandleProjectionPassivation.IHandler StartNew()
    {
        return Handler.Default;
    }
    
    private class Handler : IHandleProjectionPassivation.IHandler
    {
        public static Handler Default { get; } = new();
        
        public void SetAndMaybeRemove(string id, Action<string> remove)
        {
            
        }
    }
}