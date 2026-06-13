using System.Text.Json.Serialization;
using NetPad.Common;
using NetPad.Data;

namespace NetPad.Scripts;

/// <summary>
/// A user script.
/// </summary>
public class Script : INotifyOnPropertyChanged
{
    public const string STANDARD_EXTENSION_WO_DOT = "netpad";
    public const string STANDARD_EXTENSION = ".netpad";
    private string _name;
    private string _code;
    private string? _path;
    private DataConnection? _dataConnection;
    private bool _isDirty;

    public Script(Guid id, string name, ScriptConfig config, string code)
    {
        if (id == default)
            throw new ArgumentException($"{nameof(id)} cannot be an empty GUID");

        if (name == null)
            throw new ArgumentNullException(nameof(name));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"{nameof(name)} cannot be an empty or whitespace");

        Id = id;
        _name = name;
        _code = code;
        Config = config;
        Config.OnPropertyChanged.Add(ConfigPropertyChangedHandler);
        OnPropertyChanged = [];
    }

    public Script(Guid id, string name, ScriptConfig config) : this(id, name, config, string.Empty)
    {
    }

    [JsonIgnore] public List<Func<PropertyChangedArgs, Task>> OnPropertyChanged { get; }

    public Guid Id { get; private set; }

    public string Name
    {
        get => _name;
        private set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string? Path
    {
        get => _path;
        private set => this.RaiseAndSetIfChanged(ref _path, value);
    }

    public string? DirectoryPath => Path == null ? null : System.IO.Path.GetDirectoryName(Path);

    public bool IsNew => Path == null;

    public ScriptConfig Config { get; }

    public DataConnection? DataConnection
    {
        get => _dataConnection;
        private set => this.RaiseAndSetIfChanged(ref _dataConnection, value);
    }

    public string Code
    {
        get => _code;
        private set => this.RaiseAndSetIfChanged(ref _code, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    /// <summary>
    /// Gets a new <see cref="ScriptFingerprint"/> instance that deterministically identifies the state of this script.
    /// </summary>
    /// <returns>
    /// A <see cref="ScriptFingerprint"/> that uniquely represents the state of the script.
    /// </returns>
    public ScriptFingerprint GetFingerprint() => ScriptFingerprint.Create(this);

    public void SetName(string newName)
    {
        if (_name == newName)
            return;

        if (newName == null)
            throw new ArgumentNullException(nameof(newName));

        if (Path != null)
        {
            SetPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path)!, newName + STANDARD_EXTENSION));
        }
        else
        {
            Name = newName;
        }
    }

    public void SetPath(string path)
    {
        if (Path == path)
            return;

        if (path == null)
            throw new ArgumentNullException(nameof(path));

        var normalized = NormalizeScriptPath(path);
        Path = normalized;
        Name = GetNameFromPath(normalized);
    }

    public void UpdateCode(string? newCode)
    {
        if (Code == newCode)
            return;

        Code = newCode ?? string.Empty;
        IsDirty = true;
    }

    public void SetDataConnection(DataConnection? dataConnection)
    {
        DataConnection = dataConnection;
    }

    public void UpdateConfig(ScriptConfig config)
    {
        Config.SetKind(config.Kind);
        Config.SetTargetFrameworkVersion(config.TargetFrameworkVersion);
        Config.SetOptimizationLevel(config.OptimizationLevel);
        Config.SetUseAspNet(config.UseAspNet);
        Config.SetReferences(config.References);
        Config.SetNamespaces(config.Namespaces);
    }

    /// <summary>
    /// Returns a copy of this script with a freshly generated <see cref="Id"/>.
    /// <see cref="Path"/> is left null on the clone so the caller can decide where the new script lives.
    /// </summary>
    public Script CloneWithNewId()
    {
        var newConfig = new ScriptConfig(Config.Kind, Config.TargetFrameworkVersion);
        var clone = new Script(ScriptIdGenerator.NewId(), Name, newConfig, Code);
        clone.UpdateConfig(Config);
        clone.SetDataConnection(DataConnection);
        return clone;
    }

    /// <summary>
    /// Determines whether the supplied path refers to the same file as this script's <see cref="Path"/>,
    /// after applying the same normalization <see cref="SetPath"/> performs (appending the standard
    /// extension if missing, normalizing directory separators) and using the platform's path
    /// comparison (case-insensitive on Windows/macOS, case-sensitive on Linux/FreeBSD).
    /// </summary>
    /// <param name="path">The path to compare against. Must not be null.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="path"/> resolves to the same location as this script's
    /// <see cref="Path"/>; <c>false</c> if this script has no path (<see cref="IsNew"/> is
    /// <c>true</c>) or the paths differ.
    /// </returns>
    public bool IsPathEquivalent(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (Path == null)
        {
            return false;
        }

        return string.Equals(NormalizeScriptPath(path), Path, PlatformUtil.PathComparison);
    }

    /// <summary>
    /// Returns the path to suggest when saving this script: its current <see cref="Path"/> if it has
    /// one, otherwise a default of <paramref name="scriptsDirectoryPath"/> + <see cref="Name"/> +
    /// <see cref="STANDARD_EXTENSION"/>.
    /// </summary>
    public string GetDefaultSavePath(string scriptsDirectoryPath)
    {
        return string.IsNullOrWhiteSpace(Path)
            ? System.IO.Path.Combine(scriptsDirectoryPath, Name + STANDARD_EXTENSION)
            : Path;
    }

    public static string GetNameFromPath(string path) => System.IO.Path.GetFileNameWithoutExtension(path);

    private static string NormalizeScriptPath(string path)
    {
        if (!path.EndsWith(STANDARD_EXTENSION, PlatformUtil.PathComparison))
        {
            path += STANDARD_EXTENSION;
        }

        return FileSystemUtil.NormalizePath(path);
    }

    private Task ConfigPropertyChangedHandler(PropertyChangedArgs propertyChangedArgs)
    {
        IsDirty = true;
        return Task.CompletedTask;
    }

    public override string ToString()
    {
        return $"[{Id}] {Name}";
    }
}
