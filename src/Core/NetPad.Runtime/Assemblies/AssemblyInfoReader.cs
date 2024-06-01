using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace NetPad.Assemblies;

public sealed class AssemblyInfoReader : IDisposable
{
    private readonly Stream? _stream;
    private PEReader? _peReader;
    private MetadataReader? _metadataReader;

    public AssemblyInfoReader(string filePath)
    {
        _stream = File.OpenRead(filePath);
        InitReader(_stream);
    }

    public AssemblyInfoReader(Stream stream)
    {
        InitReader(stream);
    }

    private void InitReader(Stream stream)
    {
        Try.Run(() =>
        {
            if (stream.Length < 64)
            {
                return;
            }

            _peReader = new PEReader(stream, PEStreamOptions.LeaveOpen);
            _metadataReader = !_peReader.HasMetadata ? null : _peReader.GetMetadataReader();
        });
    }

    /// <summary>
    /// Determines if assembly is a managed assembly.
    /// </summary>
    /// <remarks>
    /// See: https://learn.microsoft.com/en-us/dotnet/standard/assembly/identify
    /// </remarks>
    public bool IsManaged()
    {
        return Try.Run(() => _metadataReader != null && _metadataReader.IsAssembly);
    }

    /// <summary>
    /// Determines if assembly is a managed assembly.
    /// </summary>
    public static bool IsManaged(string filePath)
    {
        return Try.Run(() =>
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (stream.Length < 64)
            {
                return false;
            }

            using var peReader = new PEReader(stream);

            if (!peReader.HasMetadata)
            {
                return false;
            }

            // Check that file has an assembly manifest.
            MetadataReader reader = peReader.GetMetadataReader();
            return reader.IsAssembly;
        });
    }

    /// <summary>
    /// Gets assembly version.
    /// </summary>
    /// <returns></returns>
    public Version? GetVersion()
    {
        return _metadataReader?.GetAssemblyDefinition().Version;
    }

    /// <summary>
    /// Gets <see cref="AssemblyName"/>.
    /// </summary>
    /// <returns></returns>
    public AssemblyName? GetAssemblyName()
    {
        return _metadataReader?.GetAssemblyDefinition().GetAssemblyName();
    }

    /// <summary>
    /// Gets namespaces of exported types in assembly.
    /// </summary>
    public HashSet<string> GetNamespaces()
    {
        var namespaces = new HashSet<string>();

        return Try.Run(() =>
        {
            if (_metadataReader == null) return namespaces;

            foreach (var typeDefHandle in _metadataReader!.TypeDefinitions)
            {
                var typeDef = _metadataReader.GetTypeDefinition(typeDefHandle);

                var ns = _metadataReader.GetString(typeDef.Namespace);

                if (string.IsNullOrWhiteSpace(ns))
                    continue; // If namespace is blank, it's not a user-defined type

                if (!typeDef.Attributes.HasFlag(TypeAttributes.Public))
                    continue;

                namespaces.Add(ns);
            }

            return namespaces;
        }, namespaces);
    }

    public void Dispose()
    {
        _peReader?.Dispose();
        _stream?.Dispose();
    }
}
