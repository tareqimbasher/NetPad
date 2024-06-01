using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using NetPad.DotNet;

namespace NetPad.Assemblies;

public sealed class UnloadableAssemblyLoader(ILogger<UnloadableAssemblyLoader> logger)
    : AssemblyLoadContext(isCollectible: true), IAssemblyLoader
{
    private bool _unloaded;
    private Dictionary<string, ReferencedAssemblyFile> _referenceAssemblyFiles = new();
    private Dictionary<string, AssemblyImage> _referenceAssemblyImages = new();

    public UnloadableAssemblyLoader(
        IEnumerable<AssemblyImage> referenceAssemblyImages,
        IEnumerable<string> referenceAssemblyFiles,
        ILogger<UnloadableAssemblyLoader> logger
    ) : this(logger)
    {
        WithReferenceAssemblyImages(referenceAssemblyImages);
        WithReferenceAssemblyFiles(referenceAssemblyFiles);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        Assembly? assembly = null;

        if (_referenceAssemblyImages.TryGetValue(assemblyName.FullName, out var referenceAssemblyImage))
            assembly = LoadFrom(referenceAssemblyImage.Image);

        // Try to find it by name if it can't be found by full name. Assemblies generated in memory typically have FullName
        // strings that are missing things like Version, Culture and PublicKeyToken
        else if (assemblyName.Name != null && _referenceAssemblyImages.TryGetValue(assemblyName.Name, out referenceAssemblyImage))
            assembly =  LoadFrom(referenceAssemblyImage.Image);

        else if (_referenceAssemblyFiles.TryGetValue(assemblyName.FullName, out var referenceAssemblyFile))
            assembly = LoadFrom(referenceAssemblyFile.Bytes);

        else
        {
            foreach (var assemblyFile in _referenceAssemblyFiles.Values)
            {
                if (assemblyFile.AssembliesInSameDir?.TryGetValue(assemblyName.FullName, out var r) == true)
                {
                    assembly = LoadFrom(r!.Bytes);
                    break;
                }
            }
        }

        if (assembly == null)
            assembly = base.Load(assemblyName);

        return assembly;
    }

    public Assembly LoadFrom(byte[] assemblyBytes)
    {
        logger.LogTrace($"{nameof(LoadFrom)} start");

        try
        {
            if (_unloaded)
                throw new InvalidOperationException("Assemblies have been unloaded. You cannot load a new assembly");

            // Checkout: https://github.com/natemcmaster/DotNetCorePlugins
            using var stream = new MemoryStream(assemblyBytes);
            var assembly = LoadFromStream(stream);
            return assembly;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading assembly bytes");
            throw;
        }
        finally
        {
            logger.LogTrace($"{nameof(LoadFrom)} end");
        }
    }

    public IAssemblyLoader WithReferenceAssemblyImages(IEnumerable<AssemblyImage> referenceAssemblyImages)
    {
        _referenceAssemblyImages = referenceAssemblyImages
            .Distinct()
            .ToDictionary(k => k.AssemblyName.FullName, v => v);

        return this;
    }

    public IAssemblyLoader WithReferenceAssemblyFiles(IEnumerable<string> referenceAssemblyFiles)
    {
        _referenceAssemblyFiles = referenceAssemblyFiles
            .Select(p => new ReferencedAssemblyFile(p, true))
            .Distinct()
            .ToDictionary(k => k.AssemblyName, v => v);

        return this;
    }

    public void Dispose()
    {
        logger.LogTrace($"{nameof(Dispose)} start");
        UnloadLoadedAssemblies();
        GCUtil.CollectAndWait();
        logger.LogTrace($"{nameof(Dispose)} end ");
    }

    private void UnloadLoadedAssemblies()
    {
        logger.LogTrace($"{nameof(UnloadLoadedAssemblies)} start");

        try
        {
            if (_unloaded)
            {
                logger.LogWarning("AssemblyLoadContext is unloaded, yet a call was made to unload");
                return;
            }

            Unload();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in unload");
        }
        finally
        {
            _unloaded = true;
            logger.LogTrace($"{nameof(UnloadLoadedAssemblies)} end");
        }
    }


    private class ReferencedAssemblyFile
    {
        private byte[]? _bytes;

        public ReferencedAssemblyFile(string path, bool scanOtherAssembliesInSameDir)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            AssemblyName = System.Reflection.AssemblyName.GetAssemblyName(path).FullName;

            if (scanOtherAssembliesInSameDir)
            {
                AssembliesInSameDir = Directory.GetFiles(System.IO.Path.GetDirectoryName(path)!, "*.dll")
                    .Select(dll => new ReferencedAssemblyFile(dll, false))
                    .ToDictionary(k => k.AssemblyName, v => v);
            }
        }

        public string Path { get; }
        public string AssemblyName { get; }
        public byte[] Bytes => _bytes ??= File.ReadAllBytes(Path);
        public Dictionary<string, ReferencedAssemblyFile>? AssembliesInSameDir { get; }

        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }

            // Same instances must be considered as equal
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            // Must be same type
            var typeOfThis = GetType();
            var typeOfOther = obj.GetType();
            if (typeOfThis != typeOfOther)
            {
                return false;
            }

            return AssemblyName == ((ReferencedAssemblyFile)obj).AssemblyName;
        }

        public override int GetHashCode()
        {
            return AssemblyName.GetHashCode();
        }

        public static bool operator ==(ReferencedAssemblyFile? left, ReferencedAssemblyFile? right)
        {
            if (Equals(left, null))
            {
                return Equals(right, null);
            }

            if (Equals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(ReferencedAssemblyFile? left, ReferencedAssemblyFile? right)
        {
            return !(left == right);
        }
    }
}
