namespace MJ.Akka.Projections;

public class InvalidProjectionTypeException(Type expectedType, Type actualType, Type handler, string source) 
    : Exception($"{handler.Name} needs {source} of type {expectedType.Name}, but got {actualType.Name} instead.");