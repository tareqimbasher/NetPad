using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

namespace NetPad.DotNet;

/// <summary>
/// Represents a .NET C# project. Provides methods to create, delete, and manage a project,
/// add/remove references and other utility functions.
/// </summary>
public partial class DotNetCSharpProject
{
    private readonly IDotNetInfo _dotNetInfo;
    private readonly HashSet<Reference> _references;
    private readonly SemaphoreSlim _projectFileLock;

    /// <summary>
    /// Creates an instance of <see cref="DotNetCSharpProject"/>.
    /// </summary>
    /// <param name="dotNetInfo"></param>
    /// <param name="projectDirectoryPath">Project root directory path.</param>
    /// <param name="projectFileName">If name of the project file. '.csproj' extension will be added if not specified.</param>
    /// <param name="packageCacheDirectoryPath">The package cache directory to use when adding or removing packages.
    /// Only needed when adding or removing package references.</param>
    public DotNetCSharpProject(
        IDotNetInfo dotNetInfo,
        string projectDirectoryPath,
        string projectFileName = "project.csproj",
        string? packageCacheDirectoryPath = null)
    {
        _dotNetInfo = dotNetInfo;
        _references = [];
        _projectFileLock = new SemaphoreSlim(1, 1);

        ProjectDirectoryPath = projectDirectoryPath;
        PackageCacheDirectoryPath = packageCacheDirectoryPath;

        if (!projectFileName.EndsWithIgnoreCase(".csproj"))
            projectFileName += ".csproj";

        ProjectFilePath = Path.Combine(projectDirectoryPath, projectFileName);
    }

    /// <summary>
    /// The root directory of this project.
    /// </summary>
    public string ProjectDirectoryPath { get; }

    /// <summary>
    /// The bin directory of this project.
    /// </summary>
    public string BinDirectoryPath => Path.Combine(ProjectDirectoryPath, "bin");

    /// <summary>
    /// The path to the project file.
    /// </summary>
    public string ProjectFilePath { get; }

    /// <summary>
    /// The package cache directory to use when adding or removing packages.
    /// </summary>
    public string? PackageCacheDirectoryPath { get; }

    /// <summary>
    /// The references that are added to this project.
    /// </summary>
    public IReadOnlySet<Reference> References => _references;

    /// <summary>
    /// Returns the name of the project SDK based on the provided <see cref="DotNetSdkPack"/>.
    /// </summary>
    public static string GetProjectSdkName(DotNetSdkPack pack) =>
        pack == DotNetSdkPack.AspNetApp ? "Microsoft.NET.Sdk.Web" : "Microsoft.NET.Sdk";

    /// <summary>
    /// Creates the project on disk.
    /// </summary>
    /// <param name="targetDotNetFrameworkVersion">The .NET framework to target.</param>
    /// <param name="outputType">The output type of the project.</param>
    /// <param name="sdkPack">The SDK pack to use for this project.</param>
    /// <param name="enableNullable">If true, will enable nullable checks.</param>
    /// <param name="enableImplicitUsings">If true, will enable implicit usings.</param>
    public virtual async Task CreateAsync(
        DotNetFrameworkVersion targetDotNetFrameworkVersion,
        ProjectOutputType outputType,
        DotNetSdkPack sdkPack = DotNetSdkPack.NetApp,
        bool enableNullable = true,
        bool enableImplicitUsings = true)
    {
        Directory.CreateDirectory(ProjectDirectoryPath);

        var dotnetOutputType = outputType.ToDotNetProjectPropertyValue();

        var xml = $@"<Project Sdk=""{GetProjectSdkName(sdkPack)}"">

    <PropertyGroup>
        <OutputType>{dotnetOutputType}</OutputType>
        <TargetFramework>{targetDotNetFrameworkVersion.GetTargetFrameworkMoniker()}</TargetFramework>
        <Nullable>{(enableNullable ? "enable" : "disable")}</Nullable>
        <ImplicitUsings>{(enableImplicitUsings ? "enable" : "disable")}</ImplicitUsings>
    </PropertyGroup>

</Project>
";

        await File.WriteAllTextAsync(ProjectFilePath, xml);
    }

    /// <summary>
    /// Deletes project directory on disk.
    /// </summary>
    public Task DeleteAsync()
    {
        if (Directory.Exists(ProjectDirectoryPath))
        {
            Directory.Delete(ProjectDirectoryPath, true);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Modifies the project file by applying the specified modification action to the root "Project" XML node.
    /// </summary>
    /// <param name="modification">The modification to apply to the project's root element.</param>
    /// <exception cref="FormatException"></exception>
    /// <remarks>
    /// Note that this method locks the project file in memory, no other calls to ModifyProjectFileAsync() should
    /// take place inside the Action handler.
    /// </remarks>
    /// <example>
    /// <code>
    /// await ModifyProjectFileAsync(root =>
    /// {
    ///     // root is the "Project" XML node
    /// });
    /// </code>
    /// </example>
    public async Task ModifyProjectFileAsync(Action<XElement> modification)
    {
        await _projectFileLock.WaitAsync();

        try
        {
            var xmlDoc = XDocument.Load(ProjectFilePath);

            var root = xmlDoc.Elements("Project").FirstOrDefault();

            if (root == null)
            {
                throw new FormatException(
                    "Project XML file is not formatted correctly. Could not find the root \"Project\" XML node.");
            }

            modification(root);

            await File.WriteAllTextAsync(ProjectFilePath, xmlDoc.ToString());
        }
        finally
        {
            _projectFileLock.Release();
        }
    }

    /// <summary>
    /// Sets a project attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute to set.</param>
    /// <param name="value">The value to set for the attribute.</param>
    /// <remarks>
    /// This method modifies the project file by setting the specified attribute on the root Project XML node
    /// to the given value.
    /// </remarks>
    /// <example>
    /// <code>
    /// await SetProjectAttributeAsync("Sdk", "Microsoft.NET.Sdk");
    /// </code>
    /// </example>
    public async Task SetProjectAttributeAsync(string attributeName, object? value)
    {
        await ModifyProjectFileAsync(root => root.SetAttributeValue(attributeName, value));
    }

    /// <summary>
    /// Sets the value of a project property in the first found "PropertyGroup" XML node.
    /// </summary>
    /// <param name="propertyName">The name of the project property to set.</param>
    /// <param name="value">The value to set for the property.</param>
    /// <example>
    /// <code>
    /// await SetProjectPropertyAsync("Nullable", "enable");
    /// </code>
    /// </example>
    public async Task SetProjectPropertyAsync(string propertyName, string? value)
    {
        await ModifyProjectFileAsync(root =>
        {
            var propertyGroups = root.Elements("PropertyGroup").ToArray();

            if (propertyGroups.Length == 0)
            {
                throw new FormatException(
                    "Project XML file is not formatted correctly. Could not find a \"PropertyGroup\" XML node.");
            }

            XElement? property;

            foreach (var projectProperties in propertyGroups)
            {
                property = projectProperties.Elements(propertyName).FirstOrDefault();

                if (property != null)
                {
                    property.SetValue(value ?? string.Empty);
                    return;
                }
            }

            property = new XElement(propertyName);
            propertyGroups[0].Add(property);
        });
    }

    public async Task RestoreAsync()
    {
        EnsurePackageCacheDirectoryExists();

        using var process = Process.Start(new ProcessStartInfo(
            _dotNetInfo.LocateDotNetExecutableOrThrow(),
            $"restore \"{ProjectFilePath}\"")
        {
            UseShellExecute = false,
            WorkingDirectory = ProjectDirectoryPath,
            CreateNoWindow = true
        });

        if (process != null)
        {
            await process.WaitForExitAsync();
        }
    }

    public async Task<DotNetCliResult> BuildAsync(params string[] additionalArgs)
    {
        EnsurePackageCacheDirectoryExists();

        List<string> args = ["build"];

        if (additionalArgs.Length > 0)
        {
            args.AddRange(additionalArgs);
        }

        args.Add($"\"{ProjectFilePath}\"");

        var startInfo = new ProcessStartInfo(_dotNetInfo.LocateDotNetExecutableOrThrow(), string.Join(" ", args))
        {
            UseShellExecute = false,
            WorkingDirectory = ProjectDirectoryPath,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo);

        if (process == null)
        {
            return new DotNetCliResult(false, $"Failed to start dotnet with args: {startInfo.Arguments}");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new DotNetCliResult(process.ExitCode == 0, output, error);
    }

    public async Task<DotNetCliResult> RunAsync(params string[] additionalArgs)
    {
        EnsurePackageCacheDirectoryExists();

        List<string> args = ["run"];

        if (additionalArgs.Length > 0)
        {
            args.AddRange(additionalArgs);
        }

        args.Add($"\"{ProjectFilePath}\"");

        var startInfo = new ProcessStartInfo(_dotNetInfo.LocateDotNetExecutableOrThrow(), string.Join(" ", args))
        {
            UseShellExecute = false,
            WorkingDirectory = ProjectDirectoryPath,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo);

        if (process == null)
        {
            return new DotNetCliResult(false, $"Failed to start dotnet with args: {startInfo.Arguments}");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new DotNetCliResult(process.ExitCode == 0, output, error);
    }

    private void EnsurePackageCacheDirectoryExists()
    {
        if (PackageCacheDirectoryPath == null)
        {
            throw new InvalidOperationException($"{nameof(PackageCacheDirectoryPath)} is not set.");
        }

        if (!Directory.Exists(PackageCacheDirectoryPath)) Directory.CreateDirectory(PackageCacheDirectoryPath);
    }
}
