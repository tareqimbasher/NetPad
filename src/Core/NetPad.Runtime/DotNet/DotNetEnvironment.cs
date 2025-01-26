namespace NetPad.DotNet;

public class DotNetEnvironment
{
    private static readonly SemanticVersion _environmentVersion = new(Environment.Version);

    public SemanticVersion GetCurrentDotNetRuntimeVersion() => _environmentVersion;

    public string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}
