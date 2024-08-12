namespace DC.Akka.Projections;

public interface IProjectionFilter<in TDocument> where TDocument : notnull
{
    bool FilterEvent(object evnt);
    bool FilterDocument(TDocument? document);
}