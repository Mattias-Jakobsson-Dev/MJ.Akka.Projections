using BenchmarkDotNet.Running;

namespace MJ.Akka.Projections.Benchmarks.Columns;

public class EventsPerSecondAttribute : Attribute
{
    private readonly Func<BenchmarkCase, int> _getNumberOfMessagesPerIteration;
    private readonly Func<BenchmarkCase, int> _getNumberOfDocuments;
    
    public EventsPerSecondAttribute(string configurationParameter)
    {
        _getNumberOfMessagesPerIteration = benchmark =>
        {
            var configuration = GetParameterValue(benchmark, configurationParameter, ParameterAsConfiguration);

            return configuration.NumberOfEvents;
        };
        
        _getNumberOfDocuments = benchmark =>
        {
            var configuration = GetParameterValue(benchmark, configurationParameter, ParameterAsConfiguration);

            return configuration.NumberOfDocuments;
        };
    }

    public int GetNumberOfEventsPerIteration(BenchmarkCase benchmark)
    {
        return _getNumberOfMessagesPerIteration(benchmark);
    }
    
    public int GetNumberOfDocuments(BenchmarkCase benchmark)
    {
        return _getNumberOfDocuments(benchmark);
    }

    private static T GetParameterValue<T>(
        BenchmarkCase benchmark,
        string parameterName,
        Func<object?, T> parse)
    {
        var parameterValue = benchmark
            .Parameters
            .Items
            .FirstOrDefault(x => x.Name == parameterName)
            ?.Value;

        return parse(parameterValue);
    }
    
    private static ProjectEventsConfiguration ParameterAsConfiguration(
        object? parameterValue)
    {
        return parameterValue as ProjectEventsConfiguration 
               ?? new ProjectEventsConfiguration(1, 1);
    }
}