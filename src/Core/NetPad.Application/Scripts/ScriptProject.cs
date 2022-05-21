using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.IO;

namespace NetPad.Scripts;

public class ScriptProject
{
    private readonly ICodeParser _codeParser;
    private readonly ILogger<ScriptProject> _logger;

    public ScriptProject(Script script, ICodeParser codeParser, ILogger<ScriptProject> logger)
    {
        _codeParser = codeParser;
        _logger = logger;
        Script = script;

        ProjectDirectoryPath = Path.Combine(Path.GetTempPath(), "NetPad", script.Id.ToString());
        ProjectFilePath = Path.Combine(ProjectDirectoryPath, "script.csproj");
        ProgramFilePath = Path.Combine(ProjectDirectoryPath, "Program.cs");
    }

    public Script Script { get; }
    public string ProjectDirectoryPath { get; }
    public string ProjectFilePath { get; }
    public string ProgramFilePath { get; }
    public int UserCodeStartsOnLine { get; private set; }

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

        await UpdateProgramCodeAsync();

        foreach (var reference in Script.Config.References)
        {
            if (reference is PackageReference package)
            {
                await AddPackageAsync(package.PackageId, package.Version);
            }
        }
    }

    public async Task DeleteAsync()
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
    }

    public async Task<string> UpdateProgramCodeAsync()
    {
        var parsingResult = _codeParser.Parse(Script);

        await File.WriteAllTextAsync(ProgramFilePath, parsingResult.FullProgram);

        UserCodeStartsOnLine = parsingResult.UserCodeStartLine;
        return parsingResult.FullProgram;
    }

    public async Task AddPackageAsync(string packageId, string packageVersion)
    {
        Process.Start(new ProcessStartInfo("dotnet",
            $"add {ProjectFilePath} package {packageId} --version {packageVersion}")
        {
            UseShellExecute = false,
            WorkingDirectory = ProjectDirectoryPath,
            CreateNoWindow = true
        });


        // XDocument xmldoc = XDocument.Load(ProjectFilePath);
        // XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
        //
        // var packagesNode = xmldoc.Elements("Project").Elements()
        //     .FirstOrDefault(x => x.Name == "ItemGroup" && x.Elements().Any(xe => xe.Name == "PackageReference"));
        //
        // if (packagesNode == null)
        //     return;

        // foreach (var resource in packagesNode.Elements)
        // {
        //     Console.WriteLine($"{resource.Name} => {resource.}");
        // }

        //var project = Project.FromFile(ProjectFilePath, new ProjectOptions());
        // project.Items.FirstOrDefault(i => i.Xml.ElementName == "ItemGroup" && i.Xml.);
    }

    public async Task RemovePackageAsync(string packageId)
    {
        Process.Start(new ProcessStartInfo("dotnet",
            $"remove {ProjectFilePath} package {packageId}")
        {
            UseShellExecute = false,
            WorkingDirectory = ProjectDirectoryPath,
            CreateNoWindow = true
        });
    }
}
