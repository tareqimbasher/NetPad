using System.Runtime.InteropServices;

namespace NetPad.Application;

/// <summary>
/// Aggregated facts about the running application.
/// </summary>
/// <param name="Identifier">The application's identity and version.</param>
/// <param name="DependencyCheckResult">The outcome of checking for the .NET dependencies the app needs.</param>
/// <param name="AppDataDirectoryPath">Where this installation keeps its settings, logs and caches.</param>
/// <param name="OsDescription">The operating system the app is running on.</param>
/// <param name="OsArchitecture">The process architecture.</param>
public record AppInfo(
    AppIdentifier Identifier,
    AppDependencyCheckResult DependencyCheckResult,
    string AppDataDirectoryPath,
    string OsDescription,
    Architecture OsArchitecture);
