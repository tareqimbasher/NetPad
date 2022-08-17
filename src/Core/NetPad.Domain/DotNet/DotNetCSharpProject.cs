using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NetPad.DotNet;

/// <summary>
/// Represents a .NET C# project. Provides methods to create, delete and manage packages and assembly references.
/// </summary>
public class DotNetCSharpProject
{
    /// <summary>
    /// Creates an instance of <see cref="DotNetCSharpProject"/>.
    /// </summary>
    /// <param name="projectDirectoryPath">Project root directory path.</param>
    /// <param name="projectFileName">If name of the project file. '.csproj' extension will be added if not specified.</param>
    /// <param name="packageCacheDirectoryPath">The package cache directory to use when adding or removing packages.
    /// Only needed when adding or removing package references.</param>
    public DotNetCSharpProject(string projectDirectoryPath, string projectFileName = "project.csproj", string? packageCacheDirectoryPath = null)
    {
        ProjectDirectoryPath = projectDirectoryPath;
        PackageCacheDirectoryPath = packageCacheDirectoryPath;

        if (!projectFileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            projectFileName += ".csproj";

        ProjectFilePath = Path.Combine(projectDirectoryPath, projectFileName);
    }

    /// <summary>
    /// The root directory of this project.
    /// </summary>
    public string ProjectDirectoryPath { get; }

    /// <summary>
    /// The path to the project file.
    /// </summary>
    public string ProjectFilePath { get; }

    /// <summary>
    /// The package cache directory to use when adding or removing packages.
    /// </summary>
    public string? PackageCacheDirectoryPath { get; }

    /// <summary>
    /// Creates the project on disk.
    /// </summary>
    /// <param name="deleteExisting">If true, will delete the project directory if it already exists on disk.</param>
    public virtual async Task CreateAsync(bool deleteExisting = false)
    {
        await DeleteAsync();

        Directory.CreateDirectory(ProjectDirectoryPath);

        string xml = @"<Project Sdk=""Microsoft.NET.Sdk"">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net6.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
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
            Directory.Delete(ProjectDirectoryPath, recursive: true);
        }

        return Task.CompletedTask;
    }


    /// <summary>
    /// Adds an assembly reference to the project.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly.</param>
    /// <exception cref="FormatException">Thrown if the project file XML is not formatted properly.</exception>
    public async Task AddAssemblyReferenceAsync(string assemblyPath)
    {
        var xmlDoc = XDocument.Load(ProjectFilePath);

        var root = xmlDoc.Elements("Project").FirstOrDefault();

        if (root == null)
        {
            throw new FormatException("Project XML file is not formatted correctly.");
        }

        // Check if it is already added
        if (FindAssemblyReferenceElement(assemblyPath, xmlDoc) != null)
        {
            return;
        }

        var referenceGroup = root.Elements("ItemGroup").FirstOrDefault(g => g.Elements("Reference").Any());

        if (referenceGroup == null)
        {
            referenceGroup = new XElement("ItemGroup");
            root.Add(referenceGroup);
        }

        var referenceElement = new XElement("Reference");

        referenceElement.SetAttributeValue("Include", AssemblyName.GetAssemblyName(assemblyPath).FullName);

        var hintPathElement = new XElement("HintPath");
        hintPathElement.SetValue(assemblyPath);
        referenceElement.Add(hintPathElement);

        referenceGroup.Add(referenceElement);

        await File.WriteAllTextAsync(ProjectFilePath, xmlDoc.ToString());
    }

    /// <summary>
    /// Removes an assembly reference from the project.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly.</param>
    /// <exception cref="FormatException">Thrown if the project file XML is not formatted properly.</exception>
    public async Task RemoveAssemblyReferenceAsync(string assemblyPath)
    {
        var xmlDoc = XDocument.Load(ProjectFilePath);

        var root = xmlDoc.Elements("Project").FirstOrDefault();

        if (root == null)
        {
            throw new FormatException("Project XML file is not formatted correctly.");
        }

        var referenceElementToRemove = FindAssemblyReferenceElement(assemblyPath, xmlDoc);

        if (referenceElementToRemove == null)
        {
            return;
        }

        referenceElementToRemove.Remove();

        await File.WriteAllTextAsync(ProjectFilePath, xmlDoc.ToString());
    }

    /// <summary>
    /// Adds a package reference to the project and installs it.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <param name="packageVersion">The package version. If null is passed, the latest version will be installed.</param>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="PackageCacheDirectoryPath"/> is not set.</exception>
    public async Task AddPackageAsync(string packageId, string? packageVersion)
    {
        if (PackageCacheDirectoryPath == null)
            throw new InvalidOperationException($"{nameof(PackageCacheDirectoryPath)} is not set.");

        if (!Directory.Exists(PackageCacheDirectoryPath))
            throw new InvalidOperationException($"{nameof(PackageCacheDirectoryPath)} '{PackageCacheDirectoryPath}' does not exist.");

        var versionArg = packageVersion == null ? null : $"--version {packageVersion} ";

        var process = Process.Start(new ProcessStartInfo("dotnet",
            $"add {ProjectFilePath} package {packageId} " +
            $"{versionArg}" +
            $"--package-directory {PackageCacheDirectoryPath}")
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

    /// <summary>
    /// Removes a package reference from the project and uninstalls it.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="PackageCacheDirectoryPath"/> is not set.</exception>
    public async Task RemovePackageAsync(string packageId)
    {
        if (PackageCacheDirectoryPath == null)
            throw new InvalidOperationException($"{nameof(PackageCacheDirectoryPath)} is not set.");

        if (!Directory.Exists(PackageCacheDirectoryPath))
            throw new InvalidOperationException($"{nameof(PackageCacheDirectoryPath)} '{PackageCacheDirectoryPath}' does not exist.");

        var process = Process.Start(new ProcessStartInfo("dotnet",
            $"remove {ProjectFilePath} package {packageId} " +
            $"--package-directory {PackageCacheDirectoryPath}")
        {
            UseShellExecute = false,
            WorkingDirectory = ProjectDirectoryPath,
            CreateNoWindow = true
        });

        if (process != null)
        {
            await process.WaitForExitAsync();
        }

        // This is needed so that 'project.assets.json' file is updated properly
        process = Process.Start(new ProcessStartInfo("dotnet",
            $"restore {ProjectFilePath}")
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

    private XElement? FindAssemblyReferenceElement(string assemblyPath, XDocument xmlDoc)
    {
        var root = xmlDoc.Elements("Project").First();

        var itemGroups = root.Elements("ItemGroup");
        foreach (var itemGroup in itemGroups)
        {
            var assemblyReferenceElements = itemGroup.Elements("Reference");

            foreach (var assemblyReferenceElement in assemblyReferenceElements)
            {
                if (assemblyReferenceElement.Elements("HintPath").Any(hp => hp.Value == assemblyPath))
                {
                    return assemblyReferenceElement;
                }
            }
        }

        return null;
    }
}
