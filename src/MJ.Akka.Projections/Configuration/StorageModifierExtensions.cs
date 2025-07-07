using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Configuration;

public static class StorageModifierExtensions
{
    public static IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> WithModifiedStorage<TStorageSetup>(
        this IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> config,
        IModifyStorage modifier)
        where TStorageSetup : IStorageSetup
    {
        return config.WithModifiedConfig(source => source with
        {
            StorageModifiers = source.StorageModifiers.Add(modifier)
        });
    }
    
    public static IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> WithPositionStorage<TStorageSetup>(
        this IHaveConfiguration<ProjectionSystemConfiguration<TStorageSetup>> config,
        IProjectionPositionStorage positionStorage)
        where TStorageSetup : IStorageSetup
    {
        return config.WithModifiedStorage(new ChangePositionStorageModifier(positionStorage));
    }
    
    private class NewPositionStorageSetup(IStorageSetup innerSetup, IProjectionPositionStorage positionStorage) 
        : IStorageSetup
    {
        public IProjectionStorage CreateProjectionStorage()
        {
            return innerSetup.CreateProjectionStorage();
        }

        public IProjectionPositionStorage CreatePositionStorage()
        {
            return positionStorage;
        }
    }
    
    private class ChangePositionStorageModifier(IProjectionPositionStorage positionStorage) : IModifyStorage
    {
        public IStorageSetup Modify(IStorageSetup source)
        {
            return new NewPositionStorageSetup(source, positionStorage);
        }
    }
}