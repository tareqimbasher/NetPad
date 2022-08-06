using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using NetPad.Configuration;
using NetPad.IO;
using NetPad.Scripts;

namespace NetPad.Plugins.OmniSharp;

public class ScriptProject
{
    private readonly Settings _settings;
    private readonly ILogger<ScriptProject> _logger;

    public ScriptProject(
        Script script,
        Settings settings,
        ILogger<ScriptProject> logger)
    {
        _settings = settings;
        _logger = logger;
        Script = script;

        ProjectDirectoryPath = Path.Combine(Path.GetTempPath(), "NetPad", script.Id.ToString());
        ProjectFilePath = Path.Combine(ProjectDirectoryPath, "script.csproj");
        BootstrapperProgramFilePath = Path.Combine(ProjectDirectoryPath, "Bootstrapper_Program.cs");
        UserProgramFilePath = Path.Combine(ProjectDirectoryPath, "User_Program.cs");
    }

    public Script Script { get; }
    public string ProjectDirectoryPath { get; }
    public string ProjectFilePath { get; }
    public string BootstrapperProgramFilePath { get; }
    public string UserProgramFilePath { get; }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(ProjectDirectoryPath);

        if (!File.Exists(ProjectFilePath))
        {
            string projXmlTemplate = @"<Project Sdk=""Microsoft.NET.Sdk"">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net6.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
      <Reference Include=""{0}"">
        <HintPath>{1}</HintPath>
      </Reference>
    </ItemGroup>

</Project>
";

            var domainAssembly = typeof(IOutputWriter).Assembly;

            string projXml = string.Format(projXmlTemplate,
                domainAssembly.GetName().FullName,
                domainAssembly.Location);

            await File.WriteAllTextAsync(ProjectFilePath, projXml);
        }

        foreach (var reference in Script.Config.References)
        {
            if (reference is PackageReference pkgRef)
            {
                await AddPackageAsync(pkgRef.PackageId, pkgRef.Version);
            }
            else if (reference is AssemblyReference asmRef)
            {
                await AddAssemblyReferenceAsync(asmRef.AssemblyPath);
            }
        }
    }

    public Task DeleteAsync()
    {
        try
        {
            if (Directory.Exists(ProjectDirectoryPath))
            {
                Directory.Delete(ProjectDirectoryPath, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting temporary project directory at path: {ProjectDirectoryPath}", ProjectDirectoryPath);
        }

        return Task.CompletedTask;
    }

    public async Task AddAssemblyReferenceAsync(string assemblyPath)
    {
        var xmlDoc = XDocument.Load(ProjectFilePath);

        var root = xmlDoc.Elements("Project").FirstOrDefault();

        if (root == null)
        {
            throw new Exception("Project XML file is not formatted correctly.");
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

    public async Task RemoveAssemblyReferenceAsync(string assemblyPath)
    {
        var xmlDoc = XDocument.Load(ProjectFilePath);

        var root = xmlDoc.Elements("Project").FirstOrDefault();

        if (root == null)
        {
            throw new Exception("Project XML file is not formatted correctly.");
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

    public async Task AddPackageAsync(string packageId, string packageVersion)
    {
        var process = Process.Start(new ProcessStartInfo("dotnet",
            $"add {ProjectFilePath} package {packageId} " +
            $"--version {packageVersion} " +
            $"--package-directory {Path.Combine(_settings.PackageCacheDirectoryPath, "NuGet")}")
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

    public async Task RemovePackageAsync(string packageId)
    {
        var process = Process.Start(new ProcessStartInfo("dotnet",
            $"remove {ProjectFilePath} package {packageId} " +
            $"--package-directory {Path.Combine(_settings.PackageCacheDirectoryPath, "NuGet")}")
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
}
