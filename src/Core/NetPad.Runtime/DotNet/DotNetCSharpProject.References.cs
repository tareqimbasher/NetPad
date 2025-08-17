using System.IO;
using System.Reflection;
using System.Xml.Linq;
using NetPad.DotNet.References;
using NetPad.IO;

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

            var assemblyPath = ProjectDirectoryPath.CombineFilePath(assemblyImage.ConstructAssemblyFileName());

            await File.WriteAllBytesAsync(assemblyPath.Path, assemblyImage.Image);

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

            var assemblyPath = ProjectDirectoryPath.CombineFilePath(assemblyImage.ConstructAssemblyFileName());

            assemblyPath.DeleteIfExists();

            await RemoveAssemblyFromProjectAsync(assemblyPath);

            _references.Remove(reference);
        }
        finally
        {
            _projectFileLock.Release();
        }
    }

    /// <summary>
    /// Adds a NuGet package reference to the project by invoking <c>dotnet add package</c>.
    /// </summary>
    /// <param name="reference">The package to add.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for the process to exit.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the CLI invocation returns a non-zero exit code.
    /// </exception>
    private async Task AddPackageAsync(PackageReference reference, CancellationToken cancellationToken = default)
    {
        if (_references.Contains(reference))
        {
            return;
        }

        await _projectFileLock.WaitAsync();

        try
        {
            var args = new List<string>
            {
                "package",
                reference.PackageId,
                "--version", reference.Version,
                "--project", ProjectFilePath.Path
            };

            if (PackageCacheDirectoryPath is not null)
            {
                args.Add("--package-directory");
                args.Add(PackageCacheDirectoryPath.Path);
            }

            var result = await InvokeDotNetAsync(
                "add",
                args.ToArray(),
                false,
                cancellationToken);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"dotnet add package failed: {result.FormattedOutput}");
            }

            _references.Add(reference);
        }
        finally
        {
            _projectFileLock.Release();
        }
    }

    /// <summary>
    /// Removes a NuGet package reference from the project by invoking <c>dotnet remove package</c>.
    /// </summary>
    /// <param name="reference">The package to remove.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for the process to exit.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the CLI invocation returns a non-zero exit code.
    /// </exception>
    private async Task RemovePackageAsync(PackageReference reference, CancellationToken cancellationToken = default)
    {
        if (!_references.Contains(reference))
        {
            return;
        }

        await _projectFileLock.WaitAsync();

        try
        {
            var args = new[]
            {
                "package",
                reference.PackageId,
                "--project", ProjectFilePath.Path
            };

            var result = await InvokeDotNetAsync(
                "remove",
                args,
                false,
                cancellationToken);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"dotnet remove package failed: {result.FormattedOutput}");
            }

            // This is needed so that 'project.assets.json' file is updated properly
            await RestoreAsync(null, cancellationToken);

            _references.Remove(reference);
        }
        finally
        {
            _projectFileLock.Release();
        }
    }

    private async Task AddAssemblyToProjectAsync(FilePath assemblyPath)
    {
        var xmlDoc = XDocument.Load(ProjectFilePath.Path);

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

        referenceElement.SetAttributeValue("Include", AssemblyName.GetAssemblyName(assemblyPath.Path).FullName);

        var hintPathElement = new XElement("HintPath");
        hintPathElement.SetValue(assemblyPath);
        referenceElement.Add(hintPathElement);

        referenceGroup.Add(referenceElement);

        await File.WriteAllTextAsync(ProjectFilePath.Path, xmlDoc.ToString());
    }

    private async Task RemoveAssemblyFromProjectAsync(FilePath assemblyPath)
    {
        var xmlDoc = XDocument.Load(ProjectFilePath.Path);

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

        await File.WriteAllTextAsync(ProjectFilePath.Path, xmlDoc.ToString());
    }

    private XElement? FindAssemblyReferenceElement(FilePath assemblyPath, XDocument xmlDoc)
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
