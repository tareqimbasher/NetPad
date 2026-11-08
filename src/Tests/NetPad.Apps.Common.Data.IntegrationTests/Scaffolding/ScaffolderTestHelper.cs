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
        var scaffolder = new EntityFrameworkDatabaseScaffolder(
            new NoOpDataConnectionPasswordProtector(),
            _dotNetInfo,
            _settings,
            NullLogger<EntityFrameworkDatabaseScaffolder>.Instance);

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
}
