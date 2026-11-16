using Microsoft.Extensions.Logging;
using NetPad.DotNet;

namespace NetPad.Apps.ProcessSetup;

/// <summary>
/// Points the DOTNET_ROOT environment variable at the .NET installation NetPad uses, so the .NET apps it starts
/// (OmniSharp, the script-host) run on that installation too.
/// </summary>
public class DotNetRootVariable(IDotNetInfo dotNetInfo, ILogger<DotNetRootVariable> logger) : IProcessSetupStep
{
    public const string Name = "DOTNET_ROOT";

    public void ApplyToCurrentProcess()
    {
        string? rootDirectory;

        try
        {
            rootDirectory = dotNetInfo.LocateDotNetRootDirectory();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not locate the .NET installation");
            return;
        }

        if (rootDirectory != null)
        {
            Environment.SetEnvironmentVariable(Name, rootDirectory);
        }
    }
}
