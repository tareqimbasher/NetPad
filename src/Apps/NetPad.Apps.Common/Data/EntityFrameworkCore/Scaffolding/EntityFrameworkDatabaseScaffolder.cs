using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding.Transforms;
using NetPad.CodeAnalysis;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.Data.Metadata;
using NetPad.Data.Security;
using NetPad.DotNet;
using NetPad.DotNet.CodeAnalysis;
using NetPad.Embedded;
using NetPad.IO;

namespace NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

public class EntityFrameworkDatabaseScaffolder(
    IDataConnectionPasswordProtector dataConnectionPasswordProtector,
    IDotNetInfo dotNetInfo,
    Settings settings,
    ILogger<EntityFrameworkDatabaseScaffolder> logger)
{
    // Should be names that are unlikely to conflict with scaffolded entity names
    public const string DbContextName = "GeneratedDbContext";
    public const string DbContextCompiledModelName = "GeneratedDbContextModel";

    public async Task<ScaffoldResult> ScaffoldConnectionResourcesAsync(
        EntityFrameworkDatabaseConnection connection,
        DotNetFrameworkVersion targetFrameworkVersion)
    {
        var projectDirectory = AppDataProvider.TypedDataContextTempDirectoryPath.Combine(connection.Id.ToString());

        // DANGER: make sure that whatever path we use here is meant to be temporary and can be safely deleted
        projectDirectory.DeleteIfExists();

        var project = await ScaffoldToProjectAsync(
            projectDirectory,
            $"DataConnection_{connection.Id}",
            targetFrameworkVersion,
            connection);

        try
        {
            var dbModelOutputDirPath = Path.Combine(project.ProjectDirectoryPath, "DbModel");

            var model = await GetScaffoldedModelAsync(dbModelOutputDirPath);

            DoTransforms(model, connection);

            await WriteTransformedModelAsync(model);

            var buildResult = await project.BuildAsync("-c Release");

            if (!buildResult.Succeeded)
            {
                throw new Exception(
                    $"Failed to complete scaffold process. Error building scaffolded project. {buildResult.FormattedOutput}");
            }

            var assemblyFilePath = Directory.GetFiles(
                    Path.Combine(project.BinDirectoryPath, "Release"),
                    $"{Path.GetFileNameWithoutExtension(project.ProjectFilePath)}.dll",
                    SearchOption.AllDirectories)
                .FirstOrDefault();

            if (assemblyFilePath == null)
            {
                throw new Exception("Failed to complete scaffold process. Could not find output assembly.");
            }

            var databaseStructure = await GetDatabaseStructureAsync(project, model.DbContextFile.ClassName);

            return new ScaffoldResult(model, new AssemblyImage(assemblyFilePath), databaseStructure);
        }
        finally
        {
            await project.DeleteAsync();
        }
    }

    public async Task<DotNetCSharpProject> ScaffoldToProjectAsync(
        DirectoryPath projectDirectory,
        string projectName,
        DotNetFrameworkVersion targetFrameworkVersion,
        EntityFrameworkDatabaseConnection connection)
    {
        var project = await InitProjectAsync(
            projectDirectory,
            projectName,
            targetFrameworkVersion,
            connection);

        var buildResult = await project.BuildAsync();

        if (!buildResult.Succeeded)
        {
            throw new Exception(
                $"Failed to scaffold database. Error building project. {buildResult.FormattedOutput}");
        }

        var dbModelOutputDirPath = Path.Combine(project.ProjectDirectoryPath, "DbModel");

        Directory.CreateDirectory(dbModelOutputDirPath);

        await RunEfScaffoldAsync(connection, project.ProjectDirectoryPath, dbModelOutputDirPath);

        if (connection.ScaffoldOptions?.OptimizeDbContext == true)
        {
            await RunEfOptimizeAsync(project.ProjectDirectoryPath, dbModelOutputDirPath);
        }

        return project;
    }

    private async Task<DatabaseStructure?> GetDatabaseStructureAsync(
        DotNetCSharpProject project,
        string dbContextClassName)
    {
        try
        {
            await project.SetProjectPropertyAsync(
                "OutputType",
                ProjectOutputType.Executable.ToDotNetProjectPropertyValue());

            var embeddedUtilCode = AssemblyUtil.ReadEmbeddedResource(typeof(EntityFrameworkDatabaseUtil).Assembly,
                "DatabaseStructureEmbedded.cs");

            await File.WriteAllTextAsync(Path.Combine(project.ProjectDirectoryPath, "EntityFrameworkDatabaseUtil.cs"),
                embeddedUtilCode);

            await File.WriteAllTextAsync(
                Path.Combine(project.ProjectDirectoryPath, "Program.cs"),
                $$"""
                  using System;
                  using System.Text.Json;
                  using System.Text.Json.Serialization;

                  class DataConnectionProgram
                  {
                      static void Main()
                      {
                          var dbStructure = NetPad.Embedded.EntityFrameworkDatabaseUtil.GetDatabaseStructure(new {{dbContextClassName}}());

                          var json = JsonSerializer.Serialize(dbStructure, new JsonSerializerOptions
                          {
                              PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                              Converters = { new JsonStringEnumConverter() }
                          });

                          Console.WriteLine("__DB_STRUCTURE_JSON__");
                          Console.WriteLine(json);
                      }
                  }
                  """);

            var result = await project.RunAsync("-c Release", "--property WarningLevel=0", "/clp:ErrorsOnly");

            if (!result.Succeeded)
            {
                logger.LogError("Failed to get database structure. Run failed with output: {Output}",
                    result.FormattedOutput);
            }

            var output = result.Output;

            if (string.IsNullOrWhiteSpace(output))
            {
                logger.LogError("Ran database structure generation successfully but output was empty");
                return null;
            }

            try
            {
                var json = output.Split("__DB_STRUCTURE_JSON__")
                    .Last()
                    .Trim();

                return JsonSerializer.Deserialize<DatabaseStructure>(json);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Ran database structure generation successfully but failed to deserialize output: {Output}",
                    output);
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get database structure");
            return null;
        }
    }

    private async Task<DotNetCSharpProject> InitProjectAsync(
        DirectoryPath projectDirectory,
        string projectName,
        DotNetFrameworkVersion targetFrameworkVersion,
        EntityFrameworkDatabaseConnection connection)
    {
        var project = new DotNetCSharpProject(
            dotNetInfo,
            projectDirectory.Path,
            projectName,
            Path.Combine(settings.PackageCacheDirectoryPath, "NuGet"));

        await project.CreateAsync(targetFrameworkVersion, ProjectOutputType.Library, DotNetSdkPack.NetApp);

        var references =
            EntityFrameworkPackageUtils.GetRequiredReferences(connection, targetFrameworkVersion, true);

        await project.AddReferencesAsync(references);

        return project;
    }

    private async Task RunEfScaffoldAsync(
        EntityFrameworkDatabaseConnection connection,
        string projectDirectoryPath,
        string dbModelOutputDirPath)
    {
        var argList = new List<string>
        {
            "dbcontext scaffold",
            $"\"{connection.GetConnectionString(dataConnectionPasswordProtector)}\"",
            connection.EntityFrameworkProviderName,
            $"--context {DbContextName}",
            "--namespace \"\"", // Instructs tool to not wrap code in any namespace
            "--force",
            $"--output-dir \"{(PlatformUtil.IsOSWindows() ? "." : "")}{dbModelOutputDirPath.Replace(projectDirectoryPath, "").Trim('/')}\"", // Relative to proj dir
            "--no-build",
        };

        if (connection.ScaffoldOptions?.NoPluralize == true)
        {
            argList.Add("--no-pluralize");
        }

        if (connection.ScaffoldOptions?.UseDatabaseNames == true)
        {
            argList.Add("--use-database-names");
        }

        if (connection.ScaffoldOptions?.Schemas.Length > 0)
        {
            argList.AddRange(connection.ScaffoldOptions.Schemas.Select(schema => $"--schema \"{schema}\""));
        }

        if (connection.ScaffoldOptions?.Tables.Length > 0)
        {
            argList.AddRange(connection.ScaffoldOptions.Tables.Select(table => $"--table \"{table}\""));
        }

        var dotnetEfToolExe = dotNetInfo.LocateDotNetEfToolExecutableOrThrow();
        var args = string.Join(" ", argList);

        logger.LogDebug("Calling '{DotNetEfToolExe}' with args: '{Args}'", dotnetEfToolExe, args);

        var startInfo = new ProcessStartInfo(dotnetEfToolExe, args)
            .WithWorkingDirectory(projectDirectoryPath)
            .WithRedirectIO()
            .WithNoUi()
            .CopyCurrentEnvironmentVariables();

        // Add dotnet directory to the PATH because when dotnet-ef process starts, if dotnet is not in PATH
        // it will fail as dotnet-ef depends on dotnet
        var dotnetExeDir = dotNetInfo.LocateDotNetRootDirectory();
        var pathVariableVal = startInfo.EnvironmentVariables["PATH"]?.TrimEnd(':');
        startInfo.EnvironmentVariables["PATH"] = string.IsNullOrWhiteSpace(pathVariableVal)
            ? dotnetExeDir
            : $"{pathVariableVal}:{dotnetExeDir}";
        startInfo.EnvironmentVariables["DOTNET_ROOT"] = dotnetExeDir;

        var outputs = new List<string>();
        var errors = new List<string>();

        var startResult = startInfo.Run(output => outputs.Add(output), error => errors.Add(error));

        var exitCode = await startResult.WaitForExitTask;

        logger.LogDebug("Call to dotnet scaffold completed with exit code: '{ExitCode}'", exitCode);

        if (!startResult.Started)
        {
            throw new Exception("Scaffolding process failed to start");
        }

        if (exitCode != 0)
        {
            throw new Exception($"Scaffolding process failed with exit code: {exitCode}.\n" +
                                $"Output: {outputs.JoinToString("\n")}\n" +
                                $"Error: {errors.JoinToString("\n")}");
        }
    }

    private async Task RunEfOptimizeAsync(string projectDirectoryPath, string dbModelOutputDirPath)
    {
        var argList = new List<string>
        {
            "dbcontext optimize",
            "--namespace \"\"",
            $"--output-dir \"{(PlatformUtil.IsOSWindows() ? "." : "")}{dbModelOutputDirPath.Replace(projectDirectoryPath, "").Trim('/')}/CompiledModels\"" // Relative to proj dir
        };

        var dotnetEfToolExe = dotNetInfo.LocateDotNetEfToolExecutableOrThrow();
        var args = string.Join(" ", argList);

        var startInfo = new ProcessStartInfo(dotnetEfToolExe, args)
            .WithWorkingDirectory(projectDirectoryPath)
            .WithRedirectIO()
            .WithNoUi()
            .CopyCurrentEnvironmentVariables();

        var dotnetExeDir = dotNetInfo.LocateDotNetRootDirectory();
        var pathVariableVal = startInfo.EnvironmentVariables["PATH"]?.TrimEnd(':');
        startInfo.EnvironmentVariables["PATH"] = string.IsNullOrWhiteSpace(pathVariableVal)
            ? dotnetExeDir
            : $"{pathVariableVal}:{dotnetExeDir}";
        startInfo.EnvironmentVariables["DOTNET_ROOT"] = dotnetExeDir;

        var outputs = new List<string>();
        var errors = new List<string>();

        var startResult = startInfo.Run(output => outputs.Add(output), error => errors.Add(error));

        var exitCode = await startResult.WaitForExitTask;

        logger.LogDebug("Call to dotnet optimize completed with exit code: '{ExitCode}'", exitCode);

        if (!startResult.Started)
        {
            throw new Exception("Optimization of scaffolded model process failed to start");
        }

        if (exitCode != 0)
        {
            throw new Exception($"Optimization of scaffolded model process failed with exit code: {exitCode}.\n" +
                                $"Output: {outputs.JoinToString("\n")}\n" +
                                $"Error: {errors.JoinToString("\n")}");
        }
    }

    private static async Task<ScaffoldedDatabaseModel> GetScaffoldedModelAsync(string dbModelOutputDirPath)
    {
        var projectDir = new DirectoryInfo(dbModelOutputDirPath);
        if (!projectDir.Exists)
            throw new InvalidOperationException("Scaffolding has not occurred yet.");

        var files = new DirectoryInfo(dbModelOutputDirPath).GetFiles("*.cs", SearchOption.AllDirectories);

        if (files.Length == 0)
        {
            throw new InvalidOperationException("No scaffolded files found in output dir.");
        }

        var sourceFiles = new SourceCodeCollection<ScaffoldedSourceFile>();
        ScaffoldedSourceFile? dbContextFile = null;
        ScaffoldedSourceFile? dbContextCompiledModelFile = null;

        foreach (var file in files)
        {
            var sourceFile = await ParseScaffoldedSourceFileAsync(file);
            sourceFiles.Add(sourceFile);

            if (sourceFile.IsDbContext)
            {
                dbContextFile = sourceFile;
            }

            if (sourceFile.IsDbContextCompiledModel)
            {
                dbContextCompiledModelFile = sourceFile;
            }
        }

        if (dbContextFile == null)
        {
            throw new Exception("No scaffolded DbContext file found in output dir.");
        }

        return new ScaffoldedDatabaseModel(sourceFiles, dbContextFile, dbContextCompiledModelFile);
    }

    private static async Task<ScaffoldedSourceFile> ParseScaffoldedSourceFileAsync(FileInfo file)
    {
        var scaffoldedCode = await File.ReadAllTextAsync(file.FullName);

        var syntaxTreeRoot = CSharpSyntaxTree.ParseText(scaffoldedCode).GetRoot();
        var nodes = syntaxTreeRoot.DescendantNodes().ToArray();

        var usings = nodes
            .OfType<UsingDirectiveSyntax>()
            .Select(u => u.GetNamespaceString())
            .ToArray();

        var classDeclaration = nodes.OfType<ClassDeclarationSyntax>().Single();
        var classCode = scaffoldedCode.Substring(classDeclaration.Span.Start, classDeclaration.Span.Length);
        var className = classDeclaration.Identifier.ValueText;

        var isDbContext = file.Name == $"{DbContextName}.cs";
        var isDbContextCompiledModel = file.Name == $"{DbContextCompiledModelName}.cs";

        var sourceFile = new ScaffoldedSourceFile(file.FullName, className, classCode, usings)
        {
            IsDbContext = isDbContext,
            IsDbContextCompiledModel = isDbContextCompiledModel
        };

        return sourceFile;
    }

    private static void DoTransforms(ScaffoldedDatabaseModel model, EntityFrameworkDatabaseConnection connection)
    {
        new AnyDatabaseProviderTransform().Transform(model);

        if (connection is PostgreSqlDatabaseConnection)
        {
            new PostgresSqlTransform().Transform(model);
        }
    }

    private static async Task WriteTransformedModelAsync(ScaffoldedDatabaseModel model)
    {
        foreach (var file in model.SourceFiles)
        {
            await File.WriteAllTextAsync(file.Path, file.ToCodeString());
        }
    }
}
