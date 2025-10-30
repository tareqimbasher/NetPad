using System.Runtime.InteropServices;

namespace NetPad.ExecutionModel.ScriptServices;

/// <summary>
/// Information about the current script-host environment.
/// </summary>
public class HostEnvironment(int? parentPid)
{
    /// <summary>
    /// The UTC date and time the script-host process started.
    /// </summary>
    public DateTime HostStarted { get; } = DateTime.UtcNow;

    /// <summary>
    /// The process ID (PID) of the script-host process.
    /// </summary>
    public int ProcessPid => Environment.ProcessId;

    /// <summary>
    /// The process ID (PID) of the parent process that started the script-host process.
    /// </summary>
    public int? ParentPid => parentPid;

    /// <summary>
    /// The .NET runtime version the script-host process is running on.
    /// </summary>
    public Version DotNetRuntimeVersion => Environment.Version;

    /// <summary>
    /// Gets the name of the .NET installation on which the script-host process is running.
    /// </summary>
    public string FrameworkDescription => RuntimeInformation.FrameworkDescription;

    /// <summary>
    /// Gets the current platform identifier and version number.
    /// </summary>
    public OperatingSystem OSVersion => Environment.OSVersion;

    /// <summary>
    /// Gets the platform on which an app is running.
    /// </summary>
    public string RuntimeIdentifier => RuntimeInformation.RuntimeIdentifier;

    /// <summary>
    /// Gets a string that describes the operating system on which the app is running.
    /// </summary>
    public string OSDescription => RuntimeInformation.OSDescription;

    /// <summary>
    /// Gets the process architecture of the currently running app.
    /// </summary>
    public Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;

    /// <summary>
    /// Gets the platform architecture on which the current app is running.
    /// </summary>
    public Architecture OSArchitecture => RuntimeInformation.OSArchitecture;
}
