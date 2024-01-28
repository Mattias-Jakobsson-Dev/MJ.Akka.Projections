using System.Collections.Immutable;

namespace DC.Akka.Projections;

internal static class TypeExtensions
{
    public static IImmutableList<Type> GetInheritedTypes(this Type type)
    {
        var result = new List<Type>();

        var currentType = type;

        while (currentType != null)
        {
            result.Add(currentType);
                
            currentType = currentType.BaseType;
        }
        
        result.AddRange(type.GetInterfaces());

        return result.ToImmutableList();
    }
}