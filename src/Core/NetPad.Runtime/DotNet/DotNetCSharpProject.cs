using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using NetPad.IO;

namespace NetPad.DotNet;

/// <summary>
/// Represents a .NET C# project backed by a <c>.csproj</c> file.
/// Provides helpers to create/delete the project on disk, mutate the project XML,
/// manage references, and invoke common <c>dotnet</c> CLI actions.
/// </summary>
public partial class DotNetCSharpProject
{
    private readonly IDotNetInfo _dotNetInfo;
    private readonly HashSet<Reference> _references = [];
    private readonly SemaphoreSlim _projectFileLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of <see cref="DotNetCSharpProject"/>.
    /// </summary>
    /// <param name="dotNetInfo">Service used to locate the <c>dotnet</c> CLI.</param>
    /// <param name="projectDirectoryPath">Absolute path to the project’s root directory.</param>
    /// <param name="projectFileName">
    /// Name of the project file. If the <c>.csproj</c> extension is omitted, it is appended automatically.
    /// </param>
    /// <param name="packageCacheDirectoryPath">
    /// Directory to use as a package cache when adding, removing or resolving packages. Will use the default package
    /// cache if this value is <c>null</c>.
    /// </param>
    public DotNetCSharpProject(
        IDotNetInfo dotNetInfo,
        string projectDirectoryPath,
        string projectFileName = "project.csproj",
        string? packageCacheDirectoryPath = null)
    {
        _dotNetInfo = dotNetInfo;
        ProjectDirectoryPath = projectDirectoryPath;
        PackageCacheDirectoryPath = packageCacheDirectoryPath;

        if (!projectFileName.EndsWithIgnoreCase(".csproj"))
        {
            projectFileName += ".csproj";
        }

        ProjectFilePath = ProjectDirectoryPath.CombineFilePath(projectFileName);
    }

    /// <summary>
    /// Gets the root directory of this project.
    /// </summary>
    public DirectoryPath ProjectDirectoryPath { get; }

    /// <summary>
    /// Gets the full path to the project file.
    /// </summary>
    public FilePath ProjectFilePath { get; }

    /// <summary>
    /// Gets the path to the project's <c>bin</c> directory.
    /// </summary>
    public DirectoryPath BinDirectoryPath => ProjectDirectoryPath.Combine("bin");

    /// <summary>
    /// Gets the package cache directory used when adding, removing or resolving packages, if configured.
    /// </summary>
    public DirectoryPath? PackageCacheDirectoryPath { get; }

    /// <summary>
    /// Gets the set of references that have been added to this project.
    /// </summary>
    public IReadOnlySet<Reference> References => _references;

    /// <summary>
    /// Returns the SDK name to use in the project file for the specified <see cref="DotNetSdkPack"/>.
    /// </summary>
    /// <param name="pack">The SDK pack type.</param>
    /// <returns><c>Microsoft.NET.Sdk.Web</c> for ASP.NET apps; otherwise <c>Microsoft.NET.Sdk</c>.</returns>
    public static string GetProjectSdkName(DotNetSdkPack pack) => pack switch
    {
        DotNetSdkPack.AspNetApp => "Microsoft.NET.Sdk.Web",
        _ => "Microsoft.NET.Sdk"
    };

    /// <summary>
    /// Creates the project on disk by writing a minimal <c>.csproj</c>.
    /// </summary>
    /// <param name="targetDotNetFrameworkVersion">The target framework to use (e.g., <c>net8.0</c>).</param>
    /// <param name="outputType">The project's output type (e.g., Exe or Library).</param>
    /// <param name="sdkPack">The SDK pack to use for this project.</param>
    /// <param name="enableNullable">Whether to enable C# nullable context.</param>
    /// <param name="enableImplicitUsings">Whether to enable implicit global usings.</param>
    /// <remarks>
    /// Ensures the project directory exists. If a project file already exists at the computed path,
    /// its contents are overwritten.
    /// </remarks>
    public virtual async Task CreateAsync(
        DotNetFrameworkVersion targetDotNetFrameworkVersion,
        ProjectOutputType outputType,
        DotNetSdkPack sdkPack = DotNetSdkPack.NetApp,
        bool enableNullable = true,
        bool enableImplicitUsings = true)
    {
        ProjectDirectoryPath.CreateIfNotExists();

        var assemblyOutputType = outputType.ToDotNetProjectPropertyValue();

        var xml = $"""
                   <Project Sdk="{GetProjectSdkName(sdkPack)}">

                       <PropertyGroup>
                           <OutputType>{assemblyOutputType}</OutputType>
                           <TargetFramework>{targetDotNetFrameworkVersion.GetTargetFrameworkMoniker()}</TargetFramework>
                           <Nullable>{(enableNullable ? "enable" : "disable")}</Nullable>
                           <ImplicitUsings>{(enableImplicitUsings ? "enable" : "disable")}</ImplicitUsings>
                       </PropertyGroup>

                   </Project>
                   """;

        await File.WriteAllTextAsync(ProjectFilePath.Path, xml);
    }

    /// <summary>
    /// Deletes the project directory and all of its contents.
    /// </summary>
    /// <remarks>
    /// This is a destructive operation and cannot be undone.
    /// </remarks>
    public Task DeleteAsync()
    {
        ProjectDirectoryPath.DeleteIfExists();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads the project file and applies a modification to the root <c>Project</c> XML node.
    /// </summary>
    /// <param name="modification">An action that receives the root element and performs in-place edits.</param>
    /// <exception cref="FormatException">
    /// Thrown if the project XML is malformed or the root <c>Project</c> node cannot be found.
    /// </exception>
    /// <remarks>
    /// This method serializes access to the project file using an in-memory lock. Do not call
    /// <see cref="ModifyProjectFileAsync(Action&lt;XElement&gt;)"/> recursively from within <paramref name="modification"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// await ModifyProjectFileAsync(root =>
    /// {
    ///     // root is the &lt;Project&gt; element
    ///     var propertyGroup = root.Element("PropertyGroup") ?? new XElement("PropertyGroup");
    ///     root.Add(propertyGroup);
    /// });
    /// </code>
    /// </example>
    public async Task ModifyProjectFileAsync(Action<XElement> modification)
    {
        await _projectFileLock.WaitAsync();

        try
        {
            var xmlDoc = XDocument.Load(ProjectFilePath.Path);

            var root = xmlDoc.Root;
            if (root == null || root.Name.LocalName != "Project")
            {
                throw new FormatException(
                    "Project XML file is not formatted correctly. Could not find the root \"Project\" XML node.");
            }

            modification(root);

            await File.WriteAllTextAsync(ProjectFilePath.Path, xmlDoc.ToString());
        }
        finally
        {
            _projectFileLock.Release();
        }
    }

    /// <summary>
    /// Sets an attribute on the root <c>Project</c> element in the project file.
    /// </summary>
    /// <param name="attributeName">The attribute name (e.g., <c>Sdk</c>).</param>
    /// <param name="value">The attribute value, or <see langword="null"/> to remove the attribute.</param>
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
    /// Adds or updates a child element within the first <c>PropertyGroup</c> in the project file.
    /// </summary>
    /// <param name="propertyName">The element name to set (e.g., <c>Nullable</c>).</param>
    /// <param name="value">The element value, or <see langword="null"/> to set an empty value.</param>
    /// <exception cref="FormatException">
    /// Thrown if no <c>PropertyGroup</c> element exists in the project file.
    /// </exception>
    /// <example>
    /// <code>
    /// await SetProjectGroupItemAsync("Nullable", "enable");
    /// </code>
    /// </example>
    public async Task SetProjectGroupItemAsync(string propertyName, string? value)
    {
        await ModifyProjectFileAsync(root =>
        {
            var firstGroup = root.Element("PropertyGroup");
            if (firstGroup == null)
            {
                throw new FormatException(
                    "Project XML file is not formatted correctly. Could not find a \"PropertyGroup\" XML node.");
            }

            var name = XName.Get(propertyName);
            var existing = root.Elements("PropertyGroup")
                .Elements(name)
                .FirstOrDefault();

            if (existing != null)
            {
                existing.SetValue(value ?? string.Empty);
            }
            else
            {
                firstGroup.Add(new XElement(name, value ?? string.Empty));
            }
        });
    }

    /// <summary>
    /// Runs <c>dotnet restore</c> for this project and waits for completion.
    /// </summary>
    /// <param name="additionalArgs">Additional arguments to pass to the restore command.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for the process to exit.</param>
    /// <returns>
    /// A <see cref="DotNetCliResult"/> containing standard output, standard error, and a success flag
    /// (true if the process exited with code 0).
    /// </returns>
    public Task<DotNetCliResult> RestoreAsync(
        string[]? additionalArgs = null,
        CancellationToken cancellationToken = default)
        => InvokeDotNetAsync("restore", additionalArgs, true, cancellationToken);

    /// <summary>
    /// Builds the project by invoking <c>dotnet build</c>.
    /// </summary>
    /// <param name="additionalArgs">Additional arguments to pass to the build command.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for the process to exit.</param>
    /// <returns>
    /// A <see cref="DotNetCliResult"/> containing standard output, standard error, and a success flag
    /// (true if the process exited with code 0).
    /// </returns>
    public Task<DotNetCliResult> BuildAsync(
        string[]? additionalArgs = null,
        CancellationToken cancellationToken = default)
        => InvokeDotNetAsync("build", additionalArgs, true, cancellationToken);

    /// <summary>
    /// Runs the project by invoking <c>dotnet run</c>.
    /// </summary>
    /// <param name="additionalArgs">Additional arguments to pass to the run command.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for the process to exit.</param>
    /// <returns>
    /// A <see cref="DotNetCliResult"/> containing standard output, standard error, and a success flag
    /// (true if the process exited with code 0).
    /// </returns>
    public Task<DotNetCliResult> RunAsync(
        string[]? additionalArgs = null,
        CancellationToken cancellationToken = default)
        => InvokeDotNetAsync("run", additionalArgs, true, cancellationToken);

    private async Task<DotNetCliResult> InvokeDotNetAsync(
        string command,
        string[]? args = null,
        bool redirectOutput = true,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(_dotNetInfo.LocateDotNetExecutableOrThrow())
        {
            UseShellExecute = false,
            WorkingDirectory = ProjectDirectoryPath.Path,
            CreateNoWindow = true,
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = redirectOutput
        };

        psi.ArgumentList.Add(command);

        if (PackageCacheDirectoryPath != null)
        {
            psi.ArgumentList.Add($"-p:RestorePackagesPath=\"{PackageCacheDirectoryPath.Path}\"");
        }

        if (args != null)
        {
            foreach (var a in args) psi.ArgumentList.Add(a);
        }

        psi.ArgumentList.Add(ProjectFilePath.Path);

        using var process = Process.Start(psi);
        if (process == null)
        {
            return new DotNetCliResult(false, $"Failed to start: {psi.FileName} {string.Join(' ', psi.ArgumentList)}");
        }

        var stdOutTask = redirectOutput ? process.StandardOutput.ReadToEndAsync() : Task.FromResult(string.Empty);
        var stdErrTask = redirectOutput ? process.StandardError.ReadToEndAsync() : Task.FromResult(string.Empty);

        await Task.WhenAll(process.WaitForExitAsync(cancellationToken), stdOutTask, stdErrTask).ConfigureAwait(false);

        var succeeded = process.ExitCode == 0;
        return new DotNetCliResult(succeeded, await stdOutTask, await stdErrTask);
    }
}
