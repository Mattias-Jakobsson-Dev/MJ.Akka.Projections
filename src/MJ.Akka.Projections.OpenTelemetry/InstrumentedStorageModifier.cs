using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.OpenTelemetry;

/// <summary>
/// Storage modifier that wraps <see cref="IProjectionPositionStorage"/> with
/// <see cref="InstrumentedPositionStorage"/> to record OTEL metrics.
/// </summary>
internal sealed class InstrumentedStorageModifier : IModifyStorage
{
    public IStorageSetup Modify(IStorageSetup source)
    {
        return new WrappedStorageSetup(source);
    }

    private sealed class WrappedStorageSetup(IStorageSetup inner) : IStorageSetup
    {
        public IProjectionStorage CreateProjectionStorage()
        {
            return inner.CreateProjectionStorage();
        }

        public IProjectionPositionStorage CreatePositionStorage()
        {
            return new InstrumentedPositionStorage(inner.CreatePositionStorage());
        }
    }
}

