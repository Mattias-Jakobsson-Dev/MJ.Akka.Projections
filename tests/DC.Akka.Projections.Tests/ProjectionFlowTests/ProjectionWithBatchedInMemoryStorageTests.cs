using DC.Akka.Projections.Storage;

namespace DC.Akka.Projections.Tests.ProjectionFlowTests;

public class ProjectionWithBatchedInMemoryStorageTests
{
    public class With_string_id : TestProjectionBaseFlowTests<string>
    {
        protected override IProjectionStorage GetProjectionStorage()
        {
            return base.GetProjectionStorage().Batched(Sys, 100, 5);
        }
    }

    public class With_int_id : TestProjectionBaseFlowTests<int>
    {
        protected override IProjectionStorage GetProjectionStorage()
        {
            return base.GetProjectionStorage().Batched(Sys, 100, 5);
        }
    }
}