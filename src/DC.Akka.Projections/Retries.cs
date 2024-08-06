namespace DC.Akka.Projections;

internal static class Retries
{
    public static async Task<T> Run<T, TException>(
        Func<Task<T>> action,
        int maxRetries = 5,
        Action<int, TException>? retrying = null) where TException : Exception
    {
        var tries = 0;
        
        while (true)
        {
            try
            {
                return await action();
            }
            catch (TException e)
            {
                if (tries >= maxRetries)
                    throw;
                
                retrying?.Invoke(tries + 1, e);
            }

            tries++;
        }
    }
}