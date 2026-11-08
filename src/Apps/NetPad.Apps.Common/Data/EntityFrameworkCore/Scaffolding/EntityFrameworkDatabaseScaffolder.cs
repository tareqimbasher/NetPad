using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;
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

/// <summary>
/// Scaffolds a <see cref="EntityFrameworkDatabaseConnection"/>.
/// </summary>
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
            var dbModelOutputDirPath = project.ProjectDirectoryPath.Combine("DbModel");

            var model = await GetScaffoldedModelAsync(dbModelOutputDirPath.Path);

            DoTransforms(model, connection);

            await WriteTransformedModelAsync(model);

            var buildResult = await project.BuildAsync(["-c", "Release"]);

            if (!buildResult.Succeeded)
            {
                throw new Exception(
                    $"Failed to complete scaffold process. Error building scaffolded project. {buildResult.FormattedOutput}");
            }

            var assemblyFilePath = project.GetBuiltAssembly(DotNetBuildConfiguration.Release)
                                   ?? throw new Exception(
                                       "Failed to complete scaffolding process. Could not find output assembly.");

            DatabaseStructure? databaseStructure =
                await GetDatabaseStructureAsync(project, model.DbContextFile.ClassName);

            return new ScaffoldResult(model, new AssemblyImage(assemblyFilePath), databaseStructure);
        }
        finally
        {
            try
            {
                project.Delete();
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Error deleting temporary project.");
            }
        }
    }

    /// <summary>
    /// Scaffolds a database connection to a C# project at the provided directory path.
    /// </summary>
    /// <param name="projectDirectory">The project directory path.</param>
    /// <param name="projectName">The project name.</param>
    /// <param name="targetFrameworkVersion">The .NET SDK version the project targets.</param>
    /// <param name="connection">The connection to scaffold.</param>
    /// <returns>The created C# project.</returns>
    public async Task<DotNetCSharpProject> ScaffoldToProjectAsync(
        DirectoryPath projectDirectory,
        string projectName,
        DotNetFrameworkVersion targetFrameworkVersion,
        EntityFrameworkDatabaseConnection connection)
    {
        var project = await CreateNewProjectAsync(
            projectDirectory,
            projectName,
            targetFrameworkVersion,
            connection);

        await BuildProjectOrThrowAsync(project);

        var dbModelOutputDirPath = project.ProjectDirectoryPath.Combine("DbModel").CreateIfNotExists();

        var efTool = CreateEfToolInvocation(project, targetFrameworkVersion);

        await RunEfToolAsync(
            "Scaffolding",
            project.ProjectDirectoryPath.Path,
            targetFrameworkVersion,
            efTool.GetScaffoldArguments(
                connection.GetConnectionString(dataConnectionPasswordProtector),
                connection.EntityFrameworkProviderName,
                DbContextName,
                dbModelOutputDirPath.Path,
                connection.ScaffoldOptions));

        if (connection.ScaffoldOptions?.OptimizeDbContext == true)
        {
            // The tool inspects the built assembly, which doesn't yet contain the code that was just generated
            await BuildProjectOrThrowAsync(project);

            await RunEfToolAsync(
                "Optimization of scaffolded model",
                project.ProjectDirectoryPath.Path,
                targetFrameworkVersion,
                efTool.GetOptimizeArguments(dbModelOutputDirPath.Combine("CompiledModels").Path));
        }

        return project;
    }

    private static async Task BuildProjectOrThrowAsync(DotNetCSharpProject project)
    {
        var buildResult = await project.BuildAsync();

        if (!buildResult.Succeeded)
        {
            throw new Exception(
                $"Failed to scaffold database. Error building project. {buildResult.FormattedOutput}");
        }
    }

    private async Task<DatabaseStructure?> GetDatabaseStructureAsync(
        DotNetCSharpProject project,
        string dbContextClassName)
    {
        try
        {
            await project.SetProjectGroupItemAsync(
                "OutputType",
                ProjectOutputType.Executable.ToDotNetProjectPropertyValue());

            var embeddedUtilCode = AssemblyUtil.ReadEmbeddedResource(typeof(EntityFrameworkDatabaseUtil).Assembly,
                "DatabaseStructureEmbedded.cs");

            await File.WriteAllTextAsync(
                Path.Combine(project.ProjectDirectoryPath.Path, "EntityFrameworkDatabaseUtil.cs"),
                embeddedUtilCode);

            await File.WriteAllTextAsync(
                Path.Combine(project.ProjectDirectoryPath.Path, "Program.cs"),
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

            var result = await project.RunAsync(["-c", "Release", "--property", "WarningLevel=0", "/clp:ErrorsOnly"]);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to get database structure. Run failed with output: {Output}",
                    result.FormattedOutput);
                return null;
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


    private async Task<DotNetCSharpProject> CreateNewProjectAsync(
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

    private static EntityFrameworkToolInvocation CreateEfToolInvocation(
        DotNetCSharpProject project,
        DotNetFrameworkVersion targetFrameworkVersion)
    {
        var assemblyPath = project.GetBuiltAssembly(DotNetBuildConfiguration.Debug)
                           ?? throw new Exception("Failed to scaffold database. Could not find the built project assembly.");

        return new EntityFrameworkToolInvocation(
            project.ProjectDirectoryPath.Path,
            project.ProjectFilePath.FileNameWithoutExtension,
            targetFrameworkVersion.GetTargetFrameworkMoniker(),
            assemblyPath,
            project.GetPropertyGroupItemValue("Nullable"),
            EntityFrameworkToolAssets.FromAssetsFile(
                project.ProjectDirectoryPath.Combine("obj").CombineFilePath("project.assets.json").Path));
    }

    private async Task RunEfToolAsync(
        string operationName,
        string projectDirectoryPath,
        DotNetFrameworkVersion targetFrameworkVersion,
        IReadOnlyList<string> args)
    {
        var dotNetExe = dotNetInfo.LocateDotNetExecutableForFramework(targetFrameworkVersion)
                        ?? dotNetInfo.LocateDotNetExecutableOrThrow();

        var startInfo = new ProcessStartInfo(dotNetExe)
            .WithWorkingDirectory(projectDirectoryPath)
            .WithRedirectIO()
            .WithNoUi()
            .CopyCurrentEnvironmentVariables()
            .RemoveInheritedMSBuildSdkVariables();

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        // The arguments aren't logged because they include the decrypted connection string
        logger.LogDebug("{OperationName}: running ef.dll with '{DotNetExe}'", operationName, dotNetExe);

        var outputs = new List<string>();
        var errors = new List<string>();

        var startResult = startInfo.Run(output => outputs.Add(output), error => errors.Add(error));

        var exitCode = await startResult.WaitForExitTask;

        logger.LogDebug("{OperationName}: ef.dll completed with exit code: '{ExitCode}'", operationName, exitCode);

        if (!startResult.Started)
        {
            throw new Exception($"{operationName} process failed to start.\n" +
                                $"Output: {outputs.JoinToString("\n")}\n" +
                                $"Error: {errors.JoinToString("\n")}");
        }

        if (exitCode != 0)
        {
            throw new Exception($"{operationName} process failed with exit code: {exitCode}.\n" +
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
