namespace DC.Akka.Projections;

internal static class Retries
{
    public static async Task<T> Run<T, TException>(
        Func<Task<T>> action,
        int maxRetries = 5) where TException : Exception
    {
        var tries = 0;
        
        while (true)
        {
            try
            {
                return await action();
            }
            catch (TException)
            {
                if (tries >= maxRetries)
                    throw;
            }

            tries++;
        }
    }
}