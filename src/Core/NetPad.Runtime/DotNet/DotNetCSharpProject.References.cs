using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using NetPad.DotNet.References;

namespace NetPad.DotNet;

public partial class DotNetCSharpProject
{
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

            _references.Add(reference);
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
            await RestoreAsync();

            _references.Remove(reference);
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
}
