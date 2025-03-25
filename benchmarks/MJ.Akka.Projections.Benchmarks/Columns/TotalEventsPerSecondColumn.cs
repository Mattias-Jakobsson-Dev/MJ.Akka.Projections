namespace MJ.Akka.Projections.Benchmarks.Columns;

public class TotalEventsPerSecondColumn : EventsPerSecondColumn
{
    public override string Id => "total_evts/sec";
    public override string ColumnName => "Total evts/sec";
    public override int PriorityInCategory => 0;
    public override string Legend => "Total number of events handled per second";
    
    protected override double CalculateMessagesPerSecond(int numberOfEvents, int numberOfDocuments, double seconds)
    {
        return numberOfEvents / seconds;
    }
}