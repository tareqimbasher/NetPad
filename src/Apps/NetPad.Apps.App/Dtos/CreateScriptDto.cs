using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.Dtos;

public class CreateScriptDto
{
    /// <summary>
    /// An optional name for the newly created script.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The code to include in the newly created script.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// The data connection to set on the newly created script.
    /// </summary>
    public Guid? DataConnectionId { get; set; }

    /// <summary>
    /// The script kind. Defaults to Program if not specified.
    /// </summary>
    public ScriptKind? Kind { get; set; }

    /// <summary>
    /// The target .NET framework version. Defaults to the latest installed SDK version if not specified.
    /// </summary>
    public DotNetFrameworkVersion? TargetFrameworkVersion { get; set; }

    /// <summary>
    /// The compiler optimization level. Defaults to Debug if not specified.
    /// </summary>
    public OptimizationLevel? OptimizationLevel { get; set; }

    /// <summary>
    /// Whether to reference ASP.NET assemblies. Defaults to false if not specified.
    /// </summary>
    public bool? UseAspNet { get; set; }

    /// <summary>
    /// Namespaces to include in the script. If specified, replaces the default namespaces entirely.
    /// When null, the standard default namespaces are used.
    /// </summary>
    public IList<string>? Namespaces { get; set; }

    /// <summary>
    /// References (NuGet packages, assembly files) to include in the script.
    /// </summary>
    public Reference[]? References { get; set; }

    /// <summary>
    /// If true, will run the script after its created. Only respected if <see cref="Code"/> is set.
    /// </summary>
    public bool RunImmediately { get; set; }
}
