using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Extensions.Logging.Abstractions;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Configuration;
using NetPad.DotNet;
using NetPad.IO;

namespace NetPad.Apps.Common.Data.IntegrationTests.Scaffolding;

internal static class ScaffolderTestHelper
{
    // Kept between runs so that each run doesn't download the EF Core packages again
    private static readonly string _packageCacheDirectoryPath =
        Path.Combine(Path.GetTempPath(), "netpad-integration-tests", "packages");

    private static readonly Settings _settings = new(Path.GetTempPath(), _packageCacheDirectoryPath);
    private static readonly DotNetInfo _dotNetInfo = new(_settings, NullLogger<DotNetInfo>.Instance);

    /// <summary>
    /// The oldest and newest .NET versions with an SDK on this machine. The two ends are where a scaffold is most
    /// likely to break, and testing every version here would take several minutes for each provider.
    /// </summary>
    public static TheoryData<DotNetFrameworkVersion> OldestAndNewestInstalledFrameworks()
    {
        var frameworks = _dotNetInfo.GetDotNetSdkVersions()
            .Where(sdk => sdk.IsSupported())
            .Select(sdk => sdk.GetFrameworkVersion())
            .Distinct()
            .Order()
            .ToArray();

        return new TheoryData<DotNetFrameworkVersion>(new[] { frameworks.First(), frameworks.Last() }.Distinct());
    }

    public static async Task<string> ScaffoldEntityCodeAsync(
        EntityFrameworkDatabaseConnection connection,
        DotNetFrameworkVersion framework,
        string entityFileName)
    {
        var scaffolder = CreateScaffolder();

        DirectoryPath projectDirectory = Path.Combine(Path.GetTempPath(), "netpad-integration-tests", Guid.NewGuid().ToString());

        try
        {
            var project = await scaffolder.ScaffoldToProjectAsync(projectDirectory, "Scaffolded", framework, connection);

            return await File.ReadAllTextAsync(
                Path.Combine(project.ProjectDirectoryPath.Path, "DbModel", entityFileName));
        }
        finally
        {
            projectDirectory.DeleteIfExists();
        }
    }

    public static async Task AssertScaffoldsWithCompiledModelAsync(
        EntityFrameworkDatabaseConnection connection,
        DotNetFrameworkVersion framework)
    {
        var result = await CreateScaffolder().ScaffoldConnectionResourcesAsync(connection, framework);

        Assert.NotNull(result.DatabaseStructure);

        if (framework >= DotNetFrameworkVersion.DotNet9)
        {
            Assert.Contains("DbContextModelAttribute", GetAssemblyAttributeNames(result.Assembly.Image));
        }
    }

    private static EntityFrameworkDatabaseScaffolder CreateScaffolder() => new(
        new NoOpDataConnectionPasswordProtector(),
        _dotNetInfo,
        _settings,
        NullLogger<EntityFrameworkDatabaseScaffolder>.Instance);

    private static string[] GetAssemblyAttributeNames(byte[] assemblyImage)
    {
        using var peReader = new PEReader(new MemoryStream(assemblyImage));
        var metadata = peReader.GetMetadataReader();

        return metadata.GetAssemblyDefinition().GetCustomAttributes()
            .Select(handle => metadata.GetCustomAttribute(handle).Constructor)
            .Where(constructor => constructor.Kind == HandleKind.MemberReference)
            .Select(constructor => metadata.GetMemberReference((MemberReferenceHandle)constructor).Parent)
            .Where(type => type.Kind == HandleKind.TypeReference)
            .Select(type => metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)type).Name))
            .ToArray();
    }
}
