using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using NetPad.Common;
using NetPad.DotNet;

namespace NetPad.Scripts;

/// <summary>
/// Script configuration options.
/// </summary>
public class ScriptConfig(
    ScriptKind kind,
    DotNetFrameworkVersion targetFrameworkVersion,
    IList<string>? namespaces = null,
    IList<Reference>? references = null,
    OptimizationLevel optimizationLevel = OptimizationLevel.Debug,
    bool useAspNet = false)
    : INotifyOnPropertyChanged
{
    private List<string> _namespaces = namespaces?.ToList() ?? [];
    private List<Reference> _references = references?.ToList() ?? [];
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

    public ScriptConfig SetKind(ScriptKind newKind)
    {
        if (newKind != Kind)
            Kind = newKind;
        return this;
    }

    public ScriptConfig SetTargetFrameworkVersion(DotNetFrameworkVersion newTargetFrameworkVersion)
    {
        if (newTargetFrameworkVersion != TargetFrameworkVersion)
            TargetFrameworkVersion = newTargetFrameworkVersion;
        return this;
    }

    public ScriptConfig SetOptimizationLevel(OptimizationLevel level)
    {
        if (level != _optimizationLevel)
            OptimizationLevel = level;
        return this;
    }

    public ScriptConfig SetUseAspNet(bool use)
    {
        if (use != _useAspNet)
            UseAspNet = use;
        return this;
    }

    public ScriptConfig SetNamespaces(IList<string> namespaces)
    {
        if (Namespaces.SequenceEqual(namespaces))
            return this;

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
        return this;
    }

    public ScriptConfig SetReferences(IList<Reference> references)
    {
        if (References.SequenceEqual(references))
            return this;

        foreach (var reference in references)
            reference.EnsureValid();

        References = references.ToList();
        return this;
    }
}

public static class ScriptConfigDefaults
{
    public static readonly ImmutableArray<string> DefaultNamespaces =
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
