using MJ.Akka.Projections.Storage;

namespace MJ.Akka.Projections.Tests;

public class TestStorageWrapper(IStorageSetup innerSetup) : IStorageSetup
{
    private IProjectionStorage? _projectionStorage;
    private IProjectionPositionStorage? _positionStorage;
    
    public IProjectionStorage CreateProjectionStorage()
    {
        _projectionStorage = innerSetup.CreateProjectionStorage();

        return _projectionStorage;
    }

    public IProjectionPositionStorage CreatePositionStorage()
    {
        _positionStorage = innerSetup.CreatePositionStorage();

        return _positionStorage;
    }
    
    public IProjectionStorage ProjectionStorage => 
        _projectionStorage ?? throw new InvalidOperationException("Projection storage not created yet.");
    
    public IProjectionPositionStorage PositionStorage => 
        _positionStorage ?? throw new InvalidOperationException("Position storage not created yet.");
    
    public class Modifier : IModifyStorage
    {
        private TestStorageWrapper? _wrapper;
        
        public IStorageSetup Modify(IStorageSetup source)
        {
            _wrapper = new TestStorageWrapper(source);

            return _wrapper;
        }
        
        public TestStorageWrapper Wrapper => 
            _wrapper ?? throw new InvalidOperationException("Storage wrapper not created yet.");
    }
}