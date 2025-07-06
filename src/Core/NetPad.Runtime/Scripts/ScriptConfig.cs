using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using NetPad.Common;
using NetPad.DotNet;

namespace NetPad.Scripts;

/// <summary>
/// Script configuration options.
/// </summary>
/// <param name="kind"></param>
/// <param name="targetFrameworkVersion"></param>
/// <param name="namespaces"></param>
/// <param name="references"></param>
/// <param name="optimizationLevel"></param>
/// <param name="useAspNet"></param>
public class ScriptConfig(
    ScriptKind kind,
    DotNetFrameworkVersion targetFrameworkVersion,
    List<string>? namespaces = null,
    List<Reference>? references = null,
    OptimizationLevel optimizationLevel = OptimizationLevel.Debug,
    bool useAspNet = false)
    : INotifyOnPropertyChanged
{
    private List<string> _namespaces = namespaces ?? [];
    private List<Reference> _references = references ?? [];
    private ScriptKind _kind = kind;
    private DotNetFrameworkVersion _targetFrameworkVersion = targetFrameworkVersion;
    private OptimizationLevel _optimizationLevel = optimizationLevel;
    private bool _useAspNet = useAspNet;

    [JsonIgnore] public List<Func<PropertyChangedArgs, Task>> OnPropertyChanged { get; } = [];

    public ScriptKind Kind
    {
        get => _kind;
        private set => this.RaiseAndSetIfChanged(ref _kind, value);
    }

    public DotNetFrameworkVersion TargetFrameworkVersion
    {
        get => _targetFrameworkVersion;
        private set => this.RaiseAndSetIfChanged(ref _targetFrameworkVersion, value);
    }

    public OptimizationLevel OptimizationLevel
    {
        get => _optimizationLevel;
        private set => this.RaiseAndSetIfChanged(ref _optimizationLevel, value);
    }

    public bool UseAspNet
    {
        get => _useAspNet;
        private set => this.RaiseAndSetIfChanged(ref _useAspNet, value);
    }

    public List<string> Namespaces
    {
        get => _namespaces;
        private set => this.RaiseAndSetIfChanged(ref _namespaces, value);
    }

    public List<Reference> References
    {
        get => _references;
        private set => this.RaiseAndSetIfChanged(ref _references, value);
    }

    public void SetKind(ScriptKind newKind)
    {
        if (newKind == Kind)
            return;

        Kind = newKind;
    }

    public void SetTargetFrameworkVersion(DotNetFrameworkVersion newTargetFrameworkVersion)
    {
        if (newTargetFrameworkVersion == TargetFrameworkVersion)
            return;

        TargetFrameworkVersion = newTargetFrameworkVersion;
    }

    public void SetOptimizationLevel(OptimizationLevel level)
    {
        if (level == _optimizationLevel)
            return;

        OptimizationLevel = level;
    }

    public void SetUseAspNet(bool use)
    {
        if (use == _useAspNet)
            return;

        UseAspNet = use;
    }

    public void SetNamespaces(IList<string> namespaces)
    {
        if (Namespaces.SequenceEqual(namespaces))
            return;

        var clean = namespaces
            .Where(ns => !string.IsNullOrWhiteSpace(ns))
            .Select(ns => ns.Trim())
            .Distinct()
            .ToList();

        if (clean.Any(ns => ns.StartsWith("using ") || ns.EndsWith(';')))
        {
            throw new ArgumentException("Namespaces should not start with 'using ' and must not end with ';'");
        }

        Namespaces = clean;
    }

    public void SetReferences(IList<Reference> references)
    {
        if (References.SequenceEqual(references))
            return;

        foreach (var reference in references)
            reference.EnsureValid();

        References = references.ToList();
    }
}

public static class ScriptConfigDefaults
{
    public static readonly string[] DefaultNamespaces =
    [
        "System",
        "System.Collections",
        "System.Collections.Generic",
        "System.Data",
        "System.Diagnostics",
        "System.IO",
        "System.Linq",
        "System.Linq.Expressions",
        "System.Net.Http",
        "System.Reflection",
        "System.Text",
        "System.Text.RegularExpressions",
        "System.Threading",
        "System.Threading.Tasks",
        //"System.Transactions",
        "System.Xml",
        "System.Xml.Linq",
        "System.Xml.XPath"
    ];
}
