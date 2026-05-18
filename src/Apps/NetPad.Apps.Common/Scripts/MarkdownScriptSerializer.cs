using System.Text;
using Microsoft.CodeAnalysis;
using NetPad.Common;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.DotNet.References;
using NetPad.Exceptions;
using NetPad.Scripts;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NetPad.Apps.Scripts;

/// <summary>
/// Serializes and deserializes scripts using a Markdown file format with YAML frontmatter.
/// </summary>
/// <remarks>
/// File format:
/// <code>
/// ---
/// id: &lt;guid&gt;
/// kind: Program
/// targetFramework: net9.0
/// optimizationLevel: Debug
/// useAspNet: false
/// namespaces:
///   - System
///   - System.Linq
/// packages:
///   - id: Newtonsoft.Json
///     version: 13.0.1
/// assemblies:
///   - /path/to/MyLib.dll
/// dataConnection:
///   id: &lt;guid&gt;
///   type: MsSql
/// ---
///
/// ```csharp
/// Console.WriteLine("Hello, World!");
/// ```
/// </code>
/// </remarks>
public class MarkdownScriptSerializer : IScriptSerializer
{
    public ScriptFileFormat Format => ScriptFileFormat.Markdown;
    public string FileExtension => Script.MARKDOWN_EXTENSION;

    private const string FrontmatterDelimiter = "---";
    private const string CodeFenceStart = "```csharp";
    private const string CodeFenceEnd = "```";

    private static readonly ISerializer _yamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)
        .Build();

    private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public string Serialize(Script script)
    {
        var frontmatter = new ScriptFrontmatter
        {
            Id = script.Id,
            Kind = script.Config.Kind.ToString(),
            TargetFramework = script.Config.TargetFrameworkVersion.GetTargetFrameworkMoniker(),
            OptimizationLevel = script.Config.OptimizationLevel == OptimizationLevel.Release ? "Release" : "Debug",
            UseAspNet = script.Config.UseAspNet,
            Namespaces = script.Config.Namespaces.Count > 0 ? script.Config.Namespaces : null,
            Packages = script.Config.References
                .OfType<PackageReference>()
                .Select(p => new PackageFrontmatter { Id = p.PackageId, Version = p.Version })
                .ToList()
                .NullIfEmpty(),
            Assemblies = script.Config.References
                .OfType<AssemblyFileReference>()
                .Select(a => a.AssemblyPath)
                .ToList()
                .NullIfEmpty(),
            DataConnection = script.DataConnection == null ? null : new DataConnectionFrontmatter
            {
                Id = script.DataConnection.Id,
                Type = script.DataConnection.Type.ToString()
            }
        };

        var yaml = _yamlSerializer.Serialize(frontmatter).TrimEnd();

        var sb = new StringBuilder();
        sb.AppendLine(FrontmatterDelimiter);
        sb.AppendLine(yaml);
        sb.AppendLine(FrontmatterDelimiter);
        sb.AppendLine();
        sb.AppendLine(CodeFenceStart);
        sb.Append(script.Code);
        if (!string.IsNullOrEmpty(script.Code) && !script.Code.EndsWith('\n'))
            sb.AppendLine();
        sb.AppendLine(CodeFenceEnd);

        return sb.ToString();
    }

    public async Task<Script> DeserializeAsync(
        string name,
        string data,
        IDataConnectionRepository dataConnectionRepository,
        IDotNetInfo dotNetInfo)
    {
        var lines = data.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

        if (lines.Count < 2 || lines[0].Trim() != FrontmatterDelimiter)
            throw new InvalidScriptFormatException(name, "Missing frontmatter delimiter '---'.");

        var closingDelimiterIndex = lines.FindIndex(1, l => l.Trim() == FrontmatterDelimiter);
        if (closingDelimiterIndex < 0)
            throw new InvalidScriptFormatException(name, "Frontmatter is not closed with '---'.");

        var yamlContent = string.Join("\n", lines.Skip(1).Take(closingDelimiterIndex - 1));

        ScriptFrontmatter frontmatter;
        try
        {
            frontmatter = _yamlDeserializer.Deserialize<ScriptFrontmatter>(yamlContent)
                          ?? throw new InvalidScriptFormatException(name, "Frontmatter deserialized to null.");
        }
        catch (Exception ex) when (ex is not InvalidScriptFormatException)
        {
            throw new InvalidScriptFormatException(name, $"Could not parse frontmatter YAML: {ex.Message}");
        }

        if (frontmatter.Id == default)
            throw new InvalidScriptFormatException(name, "Frontmatter is missing a valid 'id' field.");

        var afterFrontmatter = lines.Skip(closingDelimiterIndex + 1).ToList();
        var codeFenceStartIndex = afterFrontmatter.FindIndex(l => l.Trim() == CodeFenceStart);
        var codeFenceEndIndex = codeFenceStartIndex >= 0
            ? afterFrontmatter.FindIndex(codeFenceStartIndex + 1, l => l.Trim() == CodeFenceEnd)
            : -1;

        string code;
        if (codeFenceStartIndex >= 0 && codeFenceEndIndex > codeFenceStartIndex)
        {
            code = string.Join("\n",
                afterFrontmatter.Skip(codeFenceStartIndex + 1).Take(codeFenceEndIndex - codeFenceStartIndex - 1));
        }
        else
        {
            code = string.Join("\n", afterFrontmatter).Trim();
        }

        var scriptConfig = BuildScriptConfig(frontmatter, dotNetInfo);
        var script = new Script(frontmatter.Id, name, scriptConfig, code);

        if (frontmatter.DataConnection != null)
        {
            var connection = await dataConnectionRepository.GetAsync(frontmatter.DataConnection.Id);
            if (connection != null)
                script.SetDataConnection(connection);
        }

        return script;
    }

    public bool TryReadSummary(string path, out Guid? id, out ScriptKind? kind)
    {
        id = null;
        kind = null;

        using var sr = File.OpenText(path);
        string? line;
        var inFrontmatter = false;

        while ((line = sr.ReadLine()) != null)
        {
            var trimmed = line.Trim();

            if (!inFrontmatter)
            {
                if (trimmed == FrontmatterDelimiter)
                    inFrontmatter = true;
                else
                    return false;
                continue;
            }

            if (trimmed == FrontmatterDelimiter)
                return id.HasValue;

            if (trimmed.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed["id:".Length..].Trim();
                if (Guid.TryParse(value, out var parsedId))
                    id = parsedId;
            }
            else if (trimmed.StartsWith("kind:", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed["kind:".Length..].Trim();
                if (Enum.TryParse<ScriptKind>(value, ignoreCase: true, out var parsedKind))
                    kind = parsedKind;
            }

            if (id.HasValue && kind.HasValue)
                return true;
        }

        return false;
    }

    private static ScriptConfig BuildScriptConfig(ScriptFrontmatter fm, IDotNetInfo dotNetInfo)
    {
        var kind = Enum.TryParse<ScriptKind>(fm.Kind, ignoreCase: true, out var parsedKind)
            ? parsedKind
            : ScriptKind.Program;

        DotNetFrameworkVersion? parsedFramework = null;
        if (fm.TargetFramework != null &&
            DotNetFrameworkVersionUtil.TryGetFrameworkVersion(fm.TargetFramework, out var tfv))
        {
            parsedFramework = tfv;
        }

        var targetFramework = parsedFramework
                              ?? dotNetInfo.GetLatestSupportedDotNetSdkVersion()?.GetFrameworkVersion()
                              ?? GlobalConsts.AppDotNetFrameworkVersion;

        var optimizationLevel = string.Equals(fm.OptimizationLevel, "Release", StringComparison.OrdinalIgnoreCase)
            ? OptimizationLevel.Release
            : OptimizationLevel.Debug;

        var references = new List<Reference>();

        foreach (var pkg in fm.Packages ?? [])
        {
            if (!string.IsNullOrWhiteSpace(pkg.Id) && !string.IsNullOrWhiteSpace(pkg.Version))
                references.Add(new PackageReference(pkg.Id, pkg.Id, pkg.Version));
        }

        foreach (var asmPath in fm.Assemblies ?? [])
        {
            if (!string.IsNullOrWhiteSpace(asmPath))
                references.Add(new AssemblyFileReference(asmPath));
        }

        return new ScriptConfig(
            kind,
            targetFramework,
            fm.Namespaces,
            references,
            optimizationLevel,
            fm.UseAspNet);
    }

    private class ScriptFrontmatter
    {
        public Guid Id { get; set; }
        public string? Kind { get; set; }
        public string? TargetFramework { get; set; }
        public string? OptimizationLevel { get; set; }
        public bool UseAspNet { get; set; }
        public List<string>? Namespaces { get; set; }
        public List<PackageFrontmatter>? Packages { get; set; }
        public List<string>? Assemblies { get; set; }
        public DataConnectionFrontmatter? DataConnection { get; set; }
    }

    private class PackageFrontmatter
    {
        public string? Id { get; set; }
        public string? Version { get; set; }
    }

    private class DataConnectionFrontmatter
    {
        public Guid Id { get; set; }
        public string? Type { get; set; }
    }
}

internal static class ListExtensions
{
    public static List<T>? NullIfEmpty<T>(this List<T> list) => list.Count == 0 ? null : list;
}
