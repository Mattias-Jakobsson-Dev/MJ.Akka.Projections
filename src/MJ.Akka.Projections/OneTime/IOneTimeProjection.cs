namespace MJ.Akka.Projections.OneTime;

public interface IOneTimeProjection<TId, TDocument> where TId : notnull where TDocument : class
{
    Task<IResult> Run(TimeSpan? timeout = null);
    
    public interface IResult
    {
        Task<TDocument?> Load(TId id);
    }
}
