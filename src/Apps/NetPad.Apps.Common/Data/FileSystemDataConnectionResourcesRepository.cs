using System.Reflection;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;

namespace NetPad.Apps.Data;

public class FileSystemDataConnectionResourcesRepository(ILogger<FileSystemDataConnectionResourcesRepository> logger)
    : IDataConnectionResourcesRepository
{
    private static readonly SemaphoreSlim _fsLock = new(1, 1);
    private const string InfoFileName = "info.json";
    private const string SchemaCompareInfoFileName = "schema-compare-info.json";

    public Task<bool> HasResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        return Task.FromResult(GetInfoFile(GetResourcesCacheDirectory(dataConnectionId, dotNetFrameworkVersion)).Exists);
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
                    var assemblyName = AssemblyName.GetAssemblyName(assemblyFile.FullName);
                    var bytes = await File.ReadAllBytesAsync(assemblyFile.FullName);

                    cached.Assembly = Task.FromResult<AssemblyImage?>(new AssemblyImage(assemblyName, bytes));
                }
            }

            if (info.SourceCode != null)
            {
                logger.LogTrace("Loaded data connection {DataConnectionId} SourceCode resource from disk", dataConnection.Id);
                cached.SourceCode = Task.FromResult(info.SourceCode);
            }

            if (info.RequiredReferences != null)
            {
                logger.LogTrace("Loaded data connection {DataConnectionId} RequiredReferences resource from disk", dataConnection.Id);
                cached.RequiredReferences = Task.FromResult(info.RequiredReferences);
            }

            return cached;
        }
        finally
        {
            _fsLock.Release();
        }
    }

    public async Task SaveAsync(
        DataConnectionResources resources,
        DotNetFrameworkVersion dotNetFrameworkVersion,
        DataConnectionResourceComponent? resourceComponent)
    {
        await _fsLock.WaitAsync();

        var dataConnectionId = resources.DataConnection.Id;

        try
        {
            var dir = GetResourcesCacheDirectory(dataConnectionId, dotNetFrameworkVersion);
            if (!dir.Exists) dir.Create();

            var info = await GetInfoAsync(dir) ?? new DiskCachedDataConnectionResourcesInfo();

            info.RecentAsOf = resources.RecentAsOf;

            if (resourceComponent is null or DataConnectionResourceComponent.Assembly)
            {
                AssemblyImage? assembly;

                if (resources.Assembly == null || (assembly = await resources.Assembly) == null)
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
            }

            if (resourceComponent is null or DataConnectionResourceComponent.SourceCode)
            {
                DataConnectionSourceCode? sourceCode;

                if (resources.SourceCode == null || (sourceCode = await resources.SourceCode) == null || sourceCode.IsEmpty())
                {
                    info.SourceCode = null;
                }
                else
                {
                    logger.LogTrace("Saving data connection {DataConnectionId} SourceCode resource to disk", dataConnectionId);
                    info.SourceCode = sourceCode;
                }
            }

            if (resourceComponent is null or DataConnectionResourceComponent.RequiredReferences)
            {
                Reference[]? references;

                if (resources.RequiredReferences == null || (references = await resources.RequiredReferences) == null || !references.Any())
                {
                    info.RequiredReferences = null;
                }
                else
                {
                    logger.LogTrace("Saving data connection {DataConnectionId} RequiredReferences resource to disk", dataConnectionId);
                    info.RequiredReferences = references;
                }
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
    private class DiskCachedDataConnectionResourcesInfo
    {
        public DateTime RecentAsOf { get; set; }
        public string? AssemblyFileName { get; set; }
        public DataConnectionSourceCode? SourceCode { get; set; }
        public Reference[]? RequiredReferences { get; set; }

        public bool IsEmpty() => AssemblyFileName == null && SourceCode?.IsEmpty() != false && RequiredReferences?.Any() != true;
    }
}
