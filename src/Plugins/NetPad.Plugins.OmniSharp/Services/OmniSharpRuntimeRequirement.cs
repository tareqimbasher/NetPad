using System.Text.Json;
using System.Text.Json.Nodes;
using NetPad.DotNet;

namespace NetPad.Plugins.OmniSharp.Services;

/// <summary>
/// The .NET runtime an OmniSharp build needs to run, as declared by the runtimeconfig.json file beside its executable.
/// </summary>
/// <param name="MajorVersion">The major version of the .NET runtime the build targets.</param>
/// <param name="AllowsNewerMajorVersion">Whether the build also runs on a .NET runtime of a newer major version.</param>
public record OmniSharpRuntimeRequirement(int MajorVersion, bool AllowsNewerMajorVersion)
{
    private static readonly JsonDocumentOptions _jsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Reads the requirement from the runtimeconfig.json file beside an OmniSharp executable.
    /// </summary>
    /// <param name="executablePath">The path of the OmniSharp executable.</param>
    /// <param name="logger">Logs why no requirement could be read.</param>
    /// <returns>
    /// The requirement, or <c>null</c> if the file is missing, cannot be read or names no .NET runtime (as for a
    /// self-contained build).
    /// </returns>
    public static OmniSharpRuntimeRequirement? Read(string executablePath, ILogger logger)
    {
        var runtimeConfigPath = Path.Combine(
            Path.GetDirectoryName(executablePath) ?? string.Empty,
            $"{Path.GetFileNameWithoutExtension(executablePath)}.runtimeconfig.json");

        if (!File.Exists(runtimeConfigPath))
        {
            logger.LogDebug("No OmniSharp runtimeconfig.json at {RuntimeConfigPath}", runtimeConfigPath);
            return null;
        }

        try
        {
            var runtimeOptions = JsonNode.Parse(File.ReadAllText(runtimeConfigPath), documentOptions: _jsonOptions)?
                ["runtimeOptions"];

            var version = FindNetCoreFramework(runtimeOptions)?["version"]?.GetValue<string>();

            if (!SemanticVersion.TryParse(version, out var semanticVersion))
            {
                logger.LogDebug("OmniSharp runtimeconfig.json at {RuntimeConfigPath} names no .NET runtime version",
                    runtimeConfigPath);
                return null;
            }

            var rollForward = runtimeOptions?["rollForward"]?.GetValue<string>();

            return new OmniSharpRuntimeRequirement(semanticVersion.Major, AllowsNewerMajor(rollForward));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not read OmniSharp runtimeconfig.json at {RuntimeConfigPath}",
                runtimeConfigPath);
            return null;
        }
    }

    /// <summary>
    /// Gets whether a .NET runtime that meets this requirement is installed in <paramref name="dotNetRoot"/>.
    /// </summary>
    /// <param name="dotNetInfo">Information about the installed .NET runtimes.</param>
    /// <param name="dotNetRoot">The .NET root OmniSharp runs from, or <c>null</c> if it is unknown.</param>
    public bool IsMet(IDotNetInfo dotNetInfo, string? dotNetRoot)
    {
        return dotNetInfo.IsRuntimeInstalled(
            DotNetRuntimeVersion.NetCoreFrameworkName,
            MajorVersion,
            dotNetRoot,
            AllowsNewerMajorVersion);
    }

    public override string ToString() =>
        AllowsNewerMajorVersion ? $".NET {MajorVersion} runtime or newer" : $".NET {MajorVersion} runtime";

    private static JsonNode? FindNetCoreFramework(JsonNode? runtimeOptions)
    {
        if (runtimeOptions?["framework"] is JsonObject framework)
        {
            return IsNetCoreFramework(framework) ? framework : null;
        }

        return (runtimeOptions?["frameworks"] as JsonArray)?
            .OfType<JsonObject>()
            .FirstOrDefault(IsNetCoreFramework);
    }

    private static bool IsNetCoreFramework(JsonObject framework) =>
        DotNetRuntimeVersion.NetCoreFrameworkName.Equals(
            framework["name"]?.GetValue<string>(),
            StringComparison.OrdinalIgnoreCase);

    private static bool AllowsNewerMajor(string? rollForward) =>
        "Major".Equals(rollForward, StringComparison.OrdinalIgnoreCase)
        || "LatestMajor".Equals(rollForward, StringComparison.OrdinalIgnoreCase);
}
