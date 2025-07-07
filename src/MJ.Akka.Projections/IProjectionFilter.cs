namespace MJ.Akka.Projections;

public interface IProjectionFilter<in TContext> where TContext : IProjectionContext
{
    bool FilterEvent(object evnt);
    bool FilterResult(TContext context);
}