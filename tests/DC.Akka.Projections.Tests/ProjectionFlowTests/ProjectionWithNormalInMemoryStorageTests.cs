namespace DC.Akka.Projections.Tests.ProjectionFlowTests;

public class ProjectionWithNormalInMemoryStorageTests
{
    public class With_string_id : TestProjectionBaseFlowTests<string>;
    
    public class With_int_id : TestProjectionBaseFlowTests<int>;
}