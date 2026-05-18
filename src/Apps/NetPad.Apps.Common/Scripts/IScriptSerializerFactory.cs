using NetPad.Configuration;
using NetPad.Scripts;

namespace NetPad.Apps.Scripts;

public interface IScriptSerializerFactory
{
    IScriptSerializer GetDefault();
    IScriptSerializer GetForFormat(ScriptFileFormat format);
    IScriptSerializer GetForPath(string path);
    bool IsKnownScriptPath(string path);
    IEnumerable<string> GetAllFileExtensions();
}

public class ScriptSerializerFactory : IScriptSerializerFactory
{
    private readonly Settings _settings;
    private readonly Dictionary<ScriptFileFormat, IScriptSerializer> _byFormat;
    private readonly Dictionary<string, IScriptSerializer> _byExtension;

    public ScriptSerializerFactory(Settings settings, IEnumerable<IScriptSerializer> serializers)
    {
        _settings = settings;
        _byFormat = serializers.ToDictionary(s => s.Format);
        _byExtension = serializers.ToDictionary(
            s => s.FileExtension,
            s => s,
            StringComparer.OrdinalIgnoreCase);
    }

    public IScriptSerializer GetDefault() => GetForFormat(_settings.DefaultScriptFileFormat);

    public IScriptSerializer GetForFormat(ScriptFileFormat format)
    {
        if (_byFormat.TryGetValue(format, out var serializer))
            return serializer;

        throw new InvalidOperationException($"No script serializer registered for format '{format}'.");
    }

    public IScriptSerializer GetForPath(string path)
    {
        foreach (var (ext, serializer) in _byExtension)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return serializer;
        }

        throw new InvalidOperationException(
            $"No script serializer registered for file '{path}'. Known extensions: {string.Join(", ", _byExtension.Keys)}");
    }

    public bool IsKnownScriptPath(string path) =>
        _byExtension.Keys.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<string> GetAllFileExtensions() => _byExtension.Keys;
}
