namespace DC.Akka.Projections.Storage.RavenDb;

public static class TypeExtensions
{
    public static bool IsOfGenericType(this Type typeToCheck, Type genericType)
    {
        while (true)
        {
            if (!genericType.IsGenericTypeDefinition)
                throw new ArgumentException("The definition needs to be a GenericTypeDefinition", nameof(genericType));

            if (typeToCheck == null || typeToCheck == typeof(object))
                return false;

            if (typeToCheck == genericType)
            {
                return true;
            }

            if ((typeToCheck.IsGenericType ? typeToCheck.GetGenericTypeDefinition() : typeToCheck) == genericType)
            {
                return true;
            }

            if (genericType.IsInterface)
            {
                if (typeToCheck.GetInterfaces().Any(i => i.IsOfGenericType(genericType)))
                    return true;
            }

            if (typeToCheck.BaseType == null)
                return false;

            typeToCheck = typeToCheck.BaseType;
        }
    }
}