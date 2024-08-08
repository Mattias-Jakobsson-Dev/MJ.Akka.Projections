namespace DC.Akka.Projections.Configuration;

public record ProjectionStreamConfiguration(
    int EventBatchSize,
    int ProjectionParallelism,
    (int Number, TimeSpan Timeout) PositionBatching,
    int MaxProjectionRetries,
    TimeSpan ProjectDocumentTimeout)
{
    public static ProjectionStreamConfiguration Default { get; } = new(
        1_000,
        100,
        (10_000, TimeSpan.FromSeconds(10)),
        5,
        TimeSpan.FromSeconds(30));
}