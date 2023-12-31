using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using NetPad.Configuration;
using NetPad.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data.EntityFrameworkCore.Scaffolding.Transforms;
using NetPad.DotNet;
using NetPad.IO;

namespace NetPad.Data.EntityFrameworkCore.Scaffolding;

public class EntityFrameworkDatabaseScaffolder
{
    private readonly DotNetFrameworkVersion _targetFrameworkVersion;
    private readonly EntityFrameworkDatabaseConnection _connection;
    private readonly IDataConnectionPasswordProtector _dataConnectionPasswordProtector;
    private readonly IDotNetInfo _dotNetInfo;
    private readonly ILogger<EntityFrameworkDatabaseScaffolder> _logger;
    private readonly DotNetCSharpProject _project;
    private readonly string _dbModelOutputDirPath;

    public const string DbContextName = "DatabaseContext";

    public EntityFrameworkDatabaseScaffolder(
        DotNetFrameworkVersion targetFrameworkVersion,
        EntityFrameworkDatabaseConnection connection,
        IDataConnectionPasswordProtector dataConnectionPasswordProtector,
        IDotNetInfo dotNetInfo,
        Settings settings,
        ILogger<EntityFrameworkDatabaseScaffolder> logger)
    {
        _targetFrameworkVersion = targetFrameworkVersion;
        _connection = connection;
        _dataConnectionPasswordProtector = dataConnectionPasswordProtector;
        _dotNetInfo = dotNetInfo;
        _logger = logger;
        _project = new DotNetCSharpProject(
            _dotNetInfo,
            AppDataProvider.TypedDataContextTempDirectoryPath.Combine(connection.Id.ToString()).Path,
            "database",
            Path.Combine(settings.PackageCacheDirectoryPath, "NuGet"));

        _dbModelOutputDirPath = Path.Combine(_project.ProjectDirectoryPath, "DbModel");
    }

    public async Task<ScaffoldedDatabaseModel> ScaffoldAsync()
    {
        await _project.CreateAsync(_targetFrameworkVersion, ProjectOutputType.Library, true);

        try
        {
            await RunEfCoreToolsAsync();

            var model = await GetScaffoldedModelAsync();

            DoTransforms(model);

            return model;
        }
        finally
        {
            await _project.DeleteAsync();
        }
    }

    private async Task RunEfCoreToolsAsync()
    {
        await File.WriteAllTextAsync(Path.Combine(_project.ProjectDirectoryPath, "Program.cs"), @"
class Program
{
    static void Main()
    {
    }
}");

        await _project.AddPackageAsync(new PackageReference(
            _connection.EntityFrameworkProviderName,
            _connection.EntityFrameworkProviderName,
            await EntityFrameworkPackageUtils.GetEntityFrameworkProviderVersionAsync(_targetFrameworkVersion, _connection.EntityFrameworkProviderName)
            ?? throw new Exception($"Could not find a version of {_connection.EntityFrameworkProviderName} to install")
        ));

        await _project.AddPackageAsync(new PackageReference(
            "Microsoft.EntityFrameworkCore.Design",
            "Microsoft.EntityFrameworkCore.Design",
            await EntityFrameworkPackageUtils.GetEntityFrameworkDesignVersionAsync(_targetFrameworkVersion)
            ?? throw new Exception("Could not find a version of Microsoft.EntityFrameworkCore.Design to install")
        ));

        Directory.CreateDirectory(_dbModelOutputDirPath);

        var args = string.Join(" ", new[]
        {
            "dbcontext scaffold",
            $"\"{_connection.GetConnectionString(_dataConnectionPasswordProtector)}\"",
            _connection.EntityFrameworkProviderName,
            $"--context {DbContextName}",
            "--namespace \"\"", // Instructs tool to not wrap code in any namespace
            "--force",
            $"--output-dir {(PlatformUtil.IsWindowsPlatform() ? "." : "")}{_dbModelOutputDirPath.Replace(_project.ProjectDirectoryPath, "").Trim('/')}" // Relative to proj dir
        });

        var dotnetEfToolExe = _dotNetInfo.LocateDotNetEfToolExecutableOrThrow();

        _logger.LogDebug("Calling '{DotNetEfToolExe}' with args: '{Args}'", dotnetEfToolExe, args);

        var startInfo = new ProcessStartInfo(dotnetEfToolExe, args)
        {
            UseShellExecute = false,
            WorkingDirectory = _project.ProjectDirectoryPath,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        // Add dotnet directory to the PATH because when dotnet-ef process starts, if dotnet is not in PATH
        // it will fail as dotnet-ef depends on dotnet
        var dotnetExeDir = _dotNetInfo.LocateDotNetRootDirectory();
        var pathVariableVal = startInfo.EnvironmentVariables["PATH"]?.TrimEnd(':');
        startInfo.EnvironmentVariables["PATH"] = string.IsNullOrWhiteSpace(pathVariableVal) ? dotnetExeDir : $"{pathVariableVal}:{dotnetExeDir}";
        startInfo.EnvironmentVariables["DOTNET_ROOT"] = dotnetExeDir;

        var process = Process.Start(startInfo);

        if (process == null)
        {
            throw new Exception("Could not start scaffolding process");
        }

        using var processIO = new ProcessIO(process);

        var toolOutput = new List<string>();
        processIO.OnOutputReceivedHandlers.Add(output =>
        {
            toolOutput.Add(output);
            return Task.CompletedTask;
        });

        processIO.OnErrorReceivedHandlers.Add(error =>
        {
            toolOutput.Add(error);
            return Task.CompletedTask;
        });

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        _logger.LogDebug("Call to dotnet scaffold completed with exit code: '{ExitCode}'", process.ExitCode);

        if (process.ExitCode != 0)
        {
            throw new Exception($"Scaffolding process failed with exit code: {process.ExitCode}. dotnet-ef tool output:\n{toolOutput.JoinToString("\n")}");
        }
    }

    private async Task<ScaffoldedDatabaseModel> GetScaffoldedModelAsync()
    {
        var projectDir = new DirectoryInfo(_dbModelOutputDirPath);
        if (!projectDir.Exists)
            throw new InvalidOperationException("Scaffolding has not occurred yet.");

        var files = new DirectoryInfo(_dbModelOutputDirPath).GetFiles("*.cs");

        if (!files.Any())
        {
            throw new InvalidOperationException("No scaffolded files found in output dir.");
        }

        var model = new ScaffoldedDatabaseModel();

        foreach (var file in files)
        {
            var sourceFile = await ParseScaffoldedSourceFileAsync(file);
            model.AddFile(sourceFile);
        }

        return model;
    }

    private async Task<ScaffoldedSourceFile> ParseScaffoldedSourceFileAsync(FileInfo file)
    {
        var scaffoldedCode = await File.ReadAllTextAsync(file.FullName);

        var syntaxTreeRoot = CSharpSyntaxTree.ParseText(scaffoldedCode).GetRoot();
        var nodes = syntaxTreeRoot.DescendantNodes().ToArray();

        var usings = new HashSet<string>();

        foreach (var usingDirective in nodes.OfType<UsingDirectiveSyntax>())
        {
            var ns = scaffoldedCode.Substring(usingDirective.Span.Start, usingDirective.Span.Length)
                .Split(' ')[1]
                .TrimEnd(';');

            usings.Add(ns);
        }

        var classDeclaration = nodes.OfType<ClassDeclarationSyntax>().Single();
        var classCode = scaffoldedCode.Substring(classDeclaration.Span.Start, classDeclaration.Span.Length);
        var className = classDeclaration.Identifier.ValueText;
        var isDbContext = classCode.Contains(" : DbContext");

        var sourceFile = new ScaffoldedSourceFile(file.FullName, className, classCode, usings)
        {
            IsDbContext = isDbContext
        };

        return sourceFile;
    }

    private void DoTransforms(ScaffoldedDatabaseModel model)
    {
        new AnyDatabaseProviderTransform().Transform(model);

        if (_connection is PostgreSqlDatabaseConnection)
        {
            new PostgresSqlTransform().Transform(model);
        }
    }
}
