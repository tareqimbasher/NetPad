using NetPad.DotNet;

namespace NetPad.Dtos;

public class HeadlessRunRequest
{
    /// <summary>
    /// The code to execute.
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// The kind of code: "csharp" or "sql".
    /// </summary>
    public string Kind { get; set; } = "csharp";

    /// <summary>
    /// NuGet package references to include.
    /// </summary>
    public PackageReferenceDto[]? Packages { get; set; }

    /// <summary>
    /// The target .NET framework version. If not specified, the latest installed SDK version is used.
    /// </summary>
    public DotNetFrameworkVersion? TargetFramework { get; set; }

    /// <summary>
    /// An optional data connection ID to use for the script.
    /// </summary>
    public Guid? DataConnectionId { get; set; }

    /// <summary>
    /// Maximum execution time in milliseconds. If not specified, no timeout is applied.
    /// </summary>
    public int? TimeoutMs { get; set; }
}
