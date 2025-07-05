using InfluxDB.Client;

namespace MJ.Akka.Projections.Storage.InfluxDb;

public class SetupInfluxDbStorage(IInfluxDBClient client, IProjectionPositionStorage positionStorage) : IStorageSetup
{
    public IProjectionStorage CreateProjectionStorage()
    {
        return new InfluxDbProjectionStorage(client);
    }

    public IProjectionPositionStorage CreatePositionStorage()
    {
        return positionStorage;
    }
}