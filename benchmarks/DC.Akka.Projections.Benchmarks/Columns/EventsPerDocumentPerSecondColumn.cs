namespace DC.Akka.Projections.Benchmarks.Columns;

public class EventsPerDocumentPerSecondColumn : EventsPerSecondColumn
{
    public override string Id => "evts/sec/doc";
    public override string ColumnName => "Evts/sec/doc";
    public override int PriorityInCategory => 1;
    public override string Legend => "Number of events per document per second";
    
    protected override double CalculateMessagesPerSecond(int numberOfEvents, int numberOfDocuments, double seconds)
    {
        return numberOfEvents / seconds / numberOfDocuments;
    }
}