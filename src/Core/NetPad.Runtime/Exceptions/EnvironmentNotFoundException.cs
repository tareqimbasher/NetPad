namespace NetPad.Exceptions;

public class EnvironmentNotFoundException(Guid scriptId) : Exception($"No environment found for script id: {scriptId}");
