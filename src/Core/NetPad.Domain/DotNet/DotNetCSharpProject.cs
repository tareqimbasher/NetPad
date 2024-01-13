using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NetPad.DotNet;

/// <summary>
/// Represents a .NET C# project. Provides methods to create, delete, and manage a project,
/// add/remove references and other utility functions.
/// </summary>
public class DotNetCSharpProject
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
        _references = new HashSet<Reference>();
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
    /// Creates the project on disk.
    /// </summary>
    /// <param name="targetDotNetFrameworkVersion">The .NET framework to target.</param>
    /// <param name="outputType">The output type of the project.</param>
    /// <param name="sdkPack">The SDK pack to use for this project.</param>
    /// <param name="deleteExisting">If true, will delete the project directory if it already exists on disk.</param>
    public virtual async Task CreateAsync(
        DotNetFrameworkVersion targetDotNetFrameworkVersion,
        ProjectOutputType outputType,
        DotNetSdkPack sdkPack = DotNetSdkPack.NetApp,
        bool deleteExisting = false)
    {
        if (deleteExisting)
        {
            await DeleteAsync();
        }

        Directory.CreateDirectory(ProjectDirectoryPath);

        string dotnetOutputType = outputType == ProjectOutputType.Executable ? "Exe" : "Library";

        string xml = $@"<Project Sdk=""{GetProjectSdkName(sdkPack)}"">

    <PropertyGroup>
        <OutputType>{dotnetOutputType}</OutputType>
        <TargetFramework>{targetDotNetFrameworkVersion.GetTargetFrameworkMoniker()}</TargetFramework>
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
                throw new FormatException("Project XML file is not formatted correctly. Could not find the root \"Project\" XML node.");
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
            var projectProperties = root.Elements("PropertyGroup").FirstOrDefault();

            if (projectProperties == null)
            {
                throw new FormatException("Project XML file is not formatted correctly. Could not find a \"PropertyGroup\" XML node.");
            }

            var property = projectProperties.Elements(propertyName).FirstOrDefault();
            if (property == null)
            {
                property = new XElement(propertyName);
                projectProperties.Add(property);
            }

            property.SetValue(value ?? string.Empty);
        });
    }

    /// <summary>
    /// Runs 'dotnet restore' on project.
    /// </summary>
    public virtual async Task RestoreAsync()
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

    /// <summary>
    /// Adds a reference to this project.
    /// </summary>
    /// <param name="reference">The reference to add.</param>
    /// <exception cref="InvalidOperationException">When the type of Reference being added is unsupported.</exception>
    public Task AddReferenceAsync(Reference reference)
    {
        if (reference is AssemblyFileReference assemblyFileReference)
        {
            return AddAssemblyFileReferenceAsync(assemblyFileReference);
        }

        if (reference is AssemblyImageReference assemblyImageReference)
        {
            return AddAssemblyImageReferenceAsync(assemblyImageReference);
        }

        if (reference is PackageReference packageReference)
        {
            return AddPackageAsync(packageReference);
        }

        throw new InvalidOperationException($"Unhandled reference type: {reference.GetType().FullName}");
    }

    /// <summary>
    /// Removes a reference to this project.
    /// </summary>
    /// <param name="reference">The reference to remove.</param>
    /// <exception cref="InvalidOperationException">When the type of Reference being removed is unsupported.</exception>
    public Task RemoveReferenceAsync(Reference reference)
    {
        if (reference is AssemblyFileReference assemblyFileReference)
        {
            return RemoveAssemblyFileReferenceAsync(assemblyFileReference);
        }

        if (reference is AssemblyImageReference assemblyImageReference)
        {
            return RemoveAssemblyImageReferenceAsync(assemblyImageReference);
        }

        if (reference is PackageReference packageReference)
        {
            return RemovePackageAsync(packageReference);
        }

        throw new InvalidOperationException($"Unhandled reference type: {reference.GetType().FullName}");
    }

    public async Task AddReferencesAsync(IEnumerable<Reference> references)
    {
        foreach (var reference in references)
        {
            await AddReferenceAsync(reference);
        }
    }

    public async Task RemoveReferencesAsync(IEnumerable<Reference> references)
    {
        foreach (var reference in references)
        {
            await RemoveReferenceAsync(reference);
        }
    }

    /// <summary>
    /// Adds an assembly file reference to the project.
    /// </summary>
    /// <param name="reference">The assembly reference to add.</param>
    /// <exception cref="FormatException">Thrown if the project file XML is not formatted properly.</exception>
    private async Task AddAssemblyFileReferenceAsync(AssemblyFileReference reference)
    {
        if (_references.Contains(reference))
        {
            return;
        }

        await _projectFileLock.WaitAsync();

        try
        {
            string assemblyPath = reference.AssemblyPath;

            await AddAssemblyToProjectAsync(assemblyPath);

            _references.Add(reference);
        }
        finally
        {
            _projectFileLock.Release();
        }
    }

    /// <summary>
    /// Removes an assembly file reference from the project.
    /// </summary>
    /// <param name="reference">The assembly reference to remove.</param>
    /// <exception cref="FormatException">Thrown if the project file XML is not formatted properly.</exception>
    private async Task RemoveAssemblyFileReferenceAsync(AssemblyFileReference reference)
    {
        if (!_references.Contains(reference))
        {
            return;
        }

        await _projectFileLock.WaitAsync();

        try
        {
            var assemblyPath = reference.AssemblyPath;

            await RemoveAssemblyFromProjectAsync(assemblyPath);

            _references.Remove(reference);
        }
        finally
        {
            _projectFileLock.Release();
        }
    }

    /// <summary>
    /// Adds an assembly image reference to the project.
    /// </summary>
    /// <param name="reference">The assembly reference to add.</param>
    /// <exception cref="FormatException">Thrown if the project file XML is not formatted properly.</exception>
    private async Task AddAssemblyImageReferenceAsync(AssemblyImageReference reference)
    {
        if (_references.Contains(reference))
        {
            return;
        }

        await _projectFileLock.WaitAsync();

        try
        {
            var assemblyImage = reference.AssemblyImage;

            var assemblyPath = Path.Combine(ProjectDirectoryPath, assemblyImage.ConstructAssemblyFileName());

            await File.WriteAllBytesAsync(assemblyPath, assemblyImage.Image);

            await AddAssemblyToProjectAsync(assemblyPath);

            _references.Add(reference);
        }
        finally
        {
            _projectFileLock.Release();
        }
    }

    /// <summary>
    /// Removes an assembly image reference to the project.
    /// </summary>
    /// <param name="reference">The assembly reference to remove.</param>
    /// <exception cref="FormatException">Thrown if the project file XML is not formatted properly.</exception>
    private async Task RemoveAssemblyImageReferenceAsync(AssemblyImageReference reference)
    {
        if (!_references.Contains(reference))
        {
            return;
        }

        await _projectFileLock.WaitAsync();

        try
        {
            var assemblyImage = reference.AssemblyImage;

            var assemblyPath = Path.Combine(ProjectDirectoryPath, assemblyImage.ConstructAssemblyFileName());

            if (File.Exists(assemblyPath))
                File.Delete(assemblyPath);

            await RemoveAssemblyFromProjectAsync(assemblyPath);

            _references.Remove(reference);
        }
        finally
        {
            _projectFileLock.Release();
        }
    }

    /// <summary>
    /// Adds a package reference to the project and installs it.
    /// </summary>
    /// <param name="reference">The package to add.</param>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="PackageCacheDirectoryPath"/> is not set.</exception>
    private async Task AddPackageAsync(PackageReference reference)
    {
        if (_references.Contains(reference))
        {
            return;
        }

        await _projectFileLock.WaitAsync();

        try
        {
            EnsurePackageCacheDirectoryExists();

            var packageId = reference.PackageId;
            var packageVersion = reference.Version;

            using var process = Process.Start(new ProcessStartInfo(_dotNetInfo.LocateDotNetExecutableOrThrow(),
                $"add \"{ProjectFilePath}\" package {packageId} " +
                $"--version {packageVersion} " +
                $"--package-directory \"{PackageCacheDirectoryPath}\"")
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
        finally
        {
            _projectFileLock.Release();
        }
    }

    /// <summary>
    /// Removes a package reference from the project and uninstalls it.
    /// </summary>
    /// <param name="reference">The package to remove.</param>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="PackageCacheDirectoryPath"/> is not set.</exception>
    private async Task RemovePackageAsync(PackageReference reference)
    {
        if (!_references.Contains(reference))
        {
            return;
        }

        await _projectFileLock.WaitAsync();

        try
        {
            EnsurePackageCacheDirectoryExists();

            var packageId = reference.PackageId;

            var dotnetExe = _dotNetInfo.LocateDotNetExecutableOrThrow();

            using var process = Process.Start(new ProcessStartInfo(dotnetExe,
                $"remove \"{ProjectFilePath}\" package {packageId}")
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
            using var process2 = Process.Start(new ProcessStartInfo(dotnetExe,
                $"restore {ProjectFilePath}")
            {
                UseShellExecute = false,
                WorkingDirectory = ProjectDirectoryPath,
                CreateNoWindow = true
            });

            if (process2 != null)
            {
                await process2.WaitForExitAsync();
            }
        }
        finally
        {
            _projectFileLock.Release();
        }
    }

    private async Task AddAssemblyToProjectAsync(string assemblyPath)
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

    private async Task RemoveAssemblyFromProjectAsync(string assemblyPath)
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

    private void EnsurePackageCacheDirectoryExists()
    {
        if (PackageCacheDirectoryPath == null)
        {
            throw new InvalidOperationException($"{nameof(PackageCacheDirectoryPath)} is not set.");
        }

        if (!Directory.Exists(PackageCacheDirectoryPath)) Directory.CreateDirectory(PackageCacheDirectoryPath);
    }

    /// <summary>
    /// Returns the name of the project SDK based on the provided <see cref="DotNetSdkPack"/>.
    /// </summary>
    public static string GetProjectSdkName(DotNetSdkPack pack)
    {
        return pack == DotNetSdkPack.AspNetApp ? "Microsoft.NET.Sdk.Web" : "Microsoft.NET.Sdk";
    }
}
