using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.Data.Metadata;
using NetPad.Data.Metadata.ChangeDetection;
using NetPad.DotNet;

namespace NetPad.Apps.Data;

public sealed class FileSystemDataConnectionResourcesRepository(ILogger<FileSystemDataConnectionResourcesRepository> logger)
    : IDataConnectionResourcesRepository
{
    private static readonly SemaphoreSlim _fsLock = new(1, 1);
    private const string InfoFileName = "info.json";
    private const string SchemaCompareInfoFileName = "schema-compare-info.json";

    public Task<bool> HasResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        return Task.FromResult(GetInfoFile(GetResourcesCacheDirectory(dataConnectionId, dotNetFrameworkVersion)).Exists);
    }

    public Task<IList<DotNetFrameworkVersion>> GetCachedDotNetFrameworkVersionsAsync(Guid dataConnectionId)
    {
        IList<DotNetFrameworkVersion> result;

        var cacheDir = GetResourcesCacheDirectory(dataConnectionId);

        if (!cacheDir.Exists)
        {
            result = Array.Empty<DotNetFrameworkVersion>();
        }
        else
        {
            var list = new List<DotNetFrameworkVersion>();

            foreach (var tfmDir in cacheDir.GetDirectories())
            {
                if (DotNetFrameworkVersionUtil.TryGetFrameworkVersion(tfmDir.Name, out var frameworkVersion))
                {
                    list.Add(frameworkVersion.Value);
                }
            }

            result = list;
        }

        return Task.FromResult(result);
    }

    public async Task<DataConnectionResources?> GetAsync(DataConnection dataConnection, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        await _fsLock.WaitAsync();

        try
        {
            var dir = GetResourcesCacheDirectory(dataConnection.Id, dotNetFrameworkVersion);

            if (!dir.Exists) return null;

            var info = await GetInfoAsync(dir);

            if (info == null) return null;

            var cached = new DataConnectionResources(dataConnection, info.RecentAsOf);

            if (info.AssemblyFileName != null)
            {
                var assemblyFile = new FileInfo(Path.Combine(dir.FullName, info.AssemblyFileName));
                if (assemblyFile.Exists)
                {
                    logger.LogTrace("Loaded data connection {DataConnectionId} Assembly resource from disk", dataConnection.Id);
                    cached.Assembly = new AssemblyImage(assemblyFile.FullName);
                }
            }

            if (info.SourceCode != null)
            {
                logger.LogTrace("Loaded data connection {DataConnectionId} SourceCode resource from disk", dataConnection.Id);
                cached.SourceCode = info.SourceCode;
            }

            if (info.RequiredReferences != null)
            {
                logger.LogTrace("Loaded data connection {DataConnectionId} RequiredReferences resource from disk", dataConnection.Id);
                cached.RequiredReferences = info.RequiredReferences;
            }

            if (info.DatabaseStructure != null)
            {
                logger.LogTrace("Loaded data connection {DataConnectionId} RequiredReferences resource from disk", dataConnection.Id);
                cached.DatabaseStructure = info.DatabaseStructure;
            }

            return cached;
        }
        finally
        {
            _fsLock.Release();
        }
    }

    public async Task SaveAsync(DataConnectionResources resources, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        await _fsLock.WaitAsync();

        var dataConnectionId = resources.DataConnection.Id;

        try
        {
            var dir = GetResourcesCacheDirectory(dataConnectionId, dotNetFrameworkVersion);
            if (!dir.Exists) dir.Create();

            var info = await GetInfoAsync(dir) ?? new DiskCachedDataConnectionResourcesInfo();

            info.RecentAsOf = resources.RecentAsOf;

            // Update Assembly
            var assembly = resources.Assembly;
            if (assembly == null)
            {
                info.AssemblyFileName = null;
                foreach (var file in dir.GetFiles().Where(f => f.Extension.EqualsIgnoreCase(".dll")))
                {
                    file.Delete();
                }
            }
            else
            {
                logger.LogTrace("Saving data connection {DataConnectionId} Assembly resource to disk", dataConnectionId);
                var assemblyFileName = assembly.ConstructAssemblyFileName();
                info.AssemblyFileName = assemblyFileName;
                await File.WriteAllBytesAsync(Path.Combine(dir.FullName, assemblyFileName), assembly.Image);
            }

            // Update source code
            var sourceCode = resources.SourceCode;
            if (sourceCode == null || sourceCode.IsEmpty())
            {
                info.SourceCode = null;
            }
            else
            {
                logger.LogTrace("Saving data connection {DataConnectionId} SourceCode resource to disk", dataConnectionId);
                info.SourceCode = sourceCode;
            }

            // Update required references
            var references = resources.RequiredReferences;
            if (references == null || references.Length == 0)
            {
                info.RequiredReferences = null;
            }
            else
            {
                logger.LogTrace("Saving data connection {DataConnectionId} RequiredReferences resource to disk", dataConnectionId);
                info.RequiredReferences = references;
            }

            // Update database structure
            var databaseStructure = resources.DatabaseStructure;
            if (databaseStructure == null)
            {
                info.DatabaseStructure = null;
            }
            else
            {
                logger.LogTrace("Saving data connection {DataConnectionId} DatabaseStructure resource to disk", dataConnectionId);
                info.DatabaseStructure = databaseStructure;
            }

            var infoFile = GetInfoFile(dir);
            if (info.IsEmpty())
            {
                if (infoFile.Exists) infoFile.Delete();
                return;
            }

            logger.LogTrace("Saving data connection {DataConnectionId} resources info file to disk", dataConnectionId);
            await File.WriteAllTextAsync(infoFile.FullName, JsonSerializer.Serialize(info));
        }
        finally
        {
            _fsLock.Release();
        }
    }

    public async Task DeleteAsync(Guid dataConnectionId)
    {
        await _fsLock.WaitAsync();

        try
        {
            var dir = GetResourcesCacheDirectory(dataConnectionId);

            if (dir.Exists)
            {
                logger.LogTrace("Deleting all data connection {DataConnectionId} cached resources", dataConnectionId);
                dir.Delete(true);
            }
        }
        finally
        {
            _fsLock.Release();
        }
    }

    public async Task DeleteAsync(Guid dataConnectionId, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        await _fsLock.WaitAsync();

        try
        {
            var dir = GetResourcesCacheDirectory(dataConnectionId, dotNetFrameworkVersion);

            if (dir.Exists)
            {
                logger.LogTrace("Deleting data connection {DataConnectionId} cached resources for .NET framework {DotNetFramework}",
                    dataConnectionId,
                    dotNetFrameworkVersion);
                dir.Delete(true);
            }

            dir = GetResourcesCacheDirectory(dataConnectionId);

            if (!dir.EnumerateDirectories().Any())
            {
                await DeleteSchemaCompareInfoAsync(dataConnectionId, true);
            }
        }
        finally
        {
            _fsLock.Release();
        }
    }

    public async Task<TSchemaCompareInfo?> GetSchemaCompareInfoAsync<TSchemaCompareInfo>(Guid dataConnectionId) where TSchemaCompareInfo : SchemaCompareInfo
    {
        var dir = GetResourcesCacheDirectory(dataConnectionId);
        if (!dir.Exists) return null;

        await _fsLock.WaitAsync();

        try
        {
            var compareInfoFile = new FileInfo(Path.Combine(dir.FullName, SchemaCompareInfoFileName));

            if (!compareInfoFile.Exists) return null;

            var json = await File.ReadAllTextAsync(compareInfoFile.FullName);

            return Try.Run(() => JsonSerializer.Deserialize<TSchemaCompareInfo>(json));
        }
        finally
        {
            _fsLock.Release();
        }
    }

    public async Task SaveSchemaCompareInfoAsync(Guid dataConnectionId, SchemaCompareInfo compareInfo)
    {
        await _fsLock.WaitAsync();

        try
        {
            var dir = GetResourcesCacheDirectory(dataConnectionId);
            if (!dir.Exists) dir.Create();

            var compareInfoFilePath = Path.Combine(dir.FullName, SchemaCompareInfoFileName);

            // Casting to object forces serializing all properties, not just props of base class
            var json = Try.Run(() => JsonSerializer.Serialize((object)compareInfo));

            await File.WriteAllTextAsync(compareInfoFilePath, json);
        }
        finally
        {
            _fsLock.Release();
        }
    }

    public async Task DeleteSchemaCompareInfoAsync(Guid dataConnectionId)
    {
        await DeleteSchemaCompareInfoAsync(dataConnectionId, false);
    }

    private async Task DeleteSchemaCompareInfoAsync(Guid dataConnectionId, bool skipLock)
    {
        if (!skipLock)
            await _fsLock.WaitAsync();

        try
        {
            var dir = GetResourcesCacheDirectory(dataConnectionId);
            if (!dir.Exists) return;

            var compareInfoFile = new FileInfo(Path.Combine(dir.FullName, SchemaCompareInfoFileName));

            if (compareInfoFile.Exists) compareInfoFile.Delete();
        }
        finally
        {
            if (!skipLock)
                _fsLock.Release();
        }
    }

    private static FileInfo GetInfoFile(DirectoryInfo directory) => new(Path.Combine(directory.FullName, InfoFileName));

    private static DirectoryInfo GetResourcesCacheDirectory(Guid dataConnectionId)
    {
        return AppDataProvider.TypedDataContextCacheDirectoryPath
            .Combine(dataConnectionId.ToString())
            .GetInfo();
    }

    private static DirectoryInfo GetResourcesCacheDirectory(Guid dataConnectionId, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        return AppDataProvider.TypedDataContextCacheDirectoryPath
            .Combine(dataConnectionId.ToString(), dotNetFrameworkVersion.GetTargetFrameworkMoniker())
            .GetInfo();
    }

    private static async Task<DiskCachedDataConnectionResourcesInfo?> GetInfoAsync(DirectoryInfo directory)
    {
        DiskCachedDataConnectionResourcesInfo? info = null;
        var infoFile = GetInfoFile(directory);

        if (infoFile.Exists)
        {
            info = await Try.RunAsync(async () =>
                JsonSerializer.Deserialize<DiskCachedDataConnectionResourcesInfo>(await File.ReadAllTextAsync(infoFile.FullName)));
        }

        return info;
    }

    /// <summary>
    /// Represents Info file contents saved on disk.
    /// </summary>
    public class DiskCachedDataConnectionResourcesInfo
    {
        public DateTime RecentAsOf { get; set; }
        public string? AssemblyFileName { get; set; }
        public DataConnectionSourceCode? SourceCode { get; set; }
        public Reference[]? RequiredReferences { get; set; }
        public DatabaseStructure? DatabaseStructure { get; set; }

        public bool IsEmpty() => AssemblyFileName == null
                                 && SourceCode?.IsEmpty() != false
                                 && RequiredReferences?.Length > 0 != true
                                 && DatabaseStructure == null;
    }
}
