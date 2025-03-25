namespace MJ.Akka.Projections.Configuration;

public interface IConfigurePart<TConfig, out TPart>
    : IHaveConfiguration<TConfig>
    where TConfig : ProjectionConfig
{
    TPart ItemUnderConfiguration { get; }
}