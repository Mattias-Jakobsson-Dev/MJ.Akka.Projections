using FluentAssertions;
using JetBrains.Annotations;
using MJ.Akka.Projections.ProjectionIds;
using MJ.Akka.Projections.Storage;
using Xunit;

namespace MJ.Akka.Projections.Tests.Storage;

public class ProjectionContextIdTests
{
    [Fact]
    public void Two_string_contexts_with_same_id_should_be_equal()
    {
        var id1 = new ProjectionContextId("projection", new SimpleIdContext<string>("id"));
        var id2 = new ProjectionContextId("projection", new SimpleIdContext<string>("id"));

        id1.Should().Be(id2);
    }
    
    [Fact]
    public void Two_complex_contexts_with_same_id_should_be_equal()
    {
        var id1 = new ProjectionContextId("projection", new ComplexIdContext("id", new ChildData(1)));
        var id2 = new ProjectionContextId("projection", new ComplexIdContext("id", new ChildData(2)));

        id1.Should().Be(id2);
    }
    
    [Fact]
    public void Two_complex_contexts_with_different_id_should_not_be_equal()
    {
        var id1 = new ProjectionContextId("projection", new ComplexIdContext("id1", new ChildData(1)));
        var id2 = new ProjectionContextId("projection", new ComplexIdContext("id2", new ChildData(1)));

        id1.Should().NotBe(id2);
    }
    
    [Fact]
    public void Two_complex_contexts_with_same_id_but_different_projection_name_should_not_be_equal()
    {
        var id1 = new ProjectionContextId("projection1", new ComplexIdContext("id", new ChildData(1)));
        var id2 = new ProjectionContextId("projection2", new ComplexIdContext("id", new ChildData(1)));

        id1.Should().NotBe(id2);
    }
    
    [Fact]
    public void Two_complex_contexts_with_same_projection_name_but_different_id_should_not_be_equal()
    {
        var id1 = new ProjectionContextId("projection", new ComplexIdContext("id1", new ChildData(1)));
        var id2 = new ProjectionContextId("projection", new ComplexIdContext("id2", new ChildData(1)));

        id1.Should().NotBe(id2);
    }
    
    [Fact]
    public void Two_complex_contexts_with_same_projection_name_same_id_but_different_child_data_should_be_gettable_as_key_from_dictionary()
    {
        var id1 = new ProjectionContextId("projection", new ComplexIdContext("id", new ChildData(1)));
        var id2 = new ProjectionContextId("projection", new ComplexIdContext("id", new ChildData(2)));
        
        var dictionary = new Dictionary<ProjectionContextId, string>
        {
            [id1] = "value"
        };

        dictionary.TryGetValue(id2, out _).Should().BeTrue();
    }
    
    [PublicAPI]
    private record ComplexIdContext(string Id, ChildData Child) : IProjectionIdContext
    {
        public virtual bool Equals(IProjectionIdContext? other)
        {
            return other is ComplexIdContext complexIdContext &&
                   Id == complexIdContext.Id;
        }

        public string GetStringRepresentation()
        {
            return Id;
        }
    }

    [PublicAPI]
    private record ChildData(int Number);
}