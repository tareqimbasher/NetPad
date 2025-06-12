namespace NetPad.DotNet;

public enum ProjectOutputType
{
    Library,
    Executable
}

public static class ProjectOutputTypeExtensions
{
    public static string ToDotNetProjectPropertyValue(this ProjectOutputType projectOutputType)
    {
        return projectOutputType == ProjectOutputType.Library ? "Library" : "Exe";
    }
}
