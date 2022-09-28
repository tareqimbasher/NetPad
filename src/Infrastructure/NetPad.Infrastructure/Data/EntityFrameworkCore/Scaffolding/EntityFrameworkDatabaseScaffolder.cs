using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.Configuration;
using NetPad.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data.EntityFrameworkCore.Scaffolding.Transforms;
using NetPad.DotNet;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Utilities;

namespace NetPad.Data.EntityFrameworkCore.Scaffolding;

public class EntityFrameworkDatabaseScaffolder
{
    private readonly EntityFrameworkDatabaseConnection _connection;
    private readonly IPackageProvider _packageProvider;
    private readonly ILogger<EntityFrameworkDatabaseScaffolder> _logger;
    private readonly DotNetCSharpProject _project;
    private readonly string _dbModelOutputDirPath;

    public const string DbContextName = "DatabaseContext";

    public EntityFrameworkDatabaseScaffolder(
        EntityFrameworkDatabaseConnection connection,
        IPackageProvider packageProvider,
        Settings settings,
        ILogger<EntityFrameworkDatabaseScaffolder> logger)
    {
        _connection = connection;
        _packageProvider = packageProvider;
        _logger = logger;
        _project = new DotNetCSharpProject(
            Path.Combine(Path.GetTempPath(), AppIdentifier.AppName, "TypedContexts", connection.Id.ToString()),
            projectFileName: "database",
            packageCacheDirectoryPath: Path.Combine(settings.PackageCacheDirectoryPath, "NuGet"));

        _dbModelOutputDirPath = Path.Combine(_project.ProjectDirectoryPath, "DbModel");
    }

    public async Task<ScaffoldedDatabaseModel> ScaffoldAsync()
    {
        await _project.CreateAsync(ProjectOutputType.Library, true);

        try
        {
            await RunEfCoreToolsAsync();

            var model = await GetScaffoldedModelAsync();

            DoTransforms(model);

            return model;
        }
        finally
        {
            //await _project.DeleteAsync();
        }
    }

    private async Task RunEfCoreToolsAsync()
    {
        await File.WriteAllTextAsync(Path.Combine(_project.ProjectDirectoryPath, "Program.cs"), @"
class Program
{
    static void Main()
    {
        // MainMethodBodyPlaceholder
    }
}");

        await _project.AddPackageAsync(new PackageReference(
            _connection.EntityFrameworkProviderName,
            _connection.EntityFrameworkProviderName,
            await EntityFrameworkPackageUtils.GetEntityFrameworkProviderVersionAsync(_packageProvider, _connection.EntityFrameworkProviderName)
            ?? throw new Exception($"Could not find a version of {_connection.EntityFrameworkProviderName} to install")
        ));

        await _project.AddPackageAsync(new PackageReference(
            "Microsoft.EntityFrameworkCore.Design",
            "Microsoft.EntityFrameworkCore.Design",
            await EntityFrameworkPackageUtils.GetEntityFrameworkDesignVersionAsync(_packageProvider)
            ?? throw new Exception($"Could not find a version of Microsoft.EntityFrameworkCore.Design to install")
        ));

        Directory.CreateDirectory(_dbModelOutputDirPath);

        var args = string.Join(" ", new[]
        {
            "dbcontext scaffold",
            $"\"{_connection.GetConnectionString()}\"",
            _connection.EntityFrameworkProviderName,
            $"--context {DbContextName}",
            $"--namespace \"\"", // Instructs tool to not wrap code in any namespace
            "--force",
            $"--output-dir {_dbModelOutputDirPath.Replace(_project.ProjectDirectoryPath, "").Trim('/')}" // Relative to proj dir
        });

        var dotnetEfToolExe = DotNetInfo.LocateDotNetEfToolExecutableOrThrow();

        _logger.LogDebug("Calling '{DotNetEfToolExe}' with args: '{Args}'", dotnetEfToolExe, args);

        var process = Process.Start(new ProcessStartInfo(dotnetEfToolExe, args)
        {
            UseShellExecute = false,
            WorkingDirectory = _project.ProjectDirectoryPath,
            CreateNoWindow = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (process == null)
        {
            throw new Exception("Could not start scaffolding process");
        }

        using var processIO = new ProcessIOHandler(process);

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
            throw new Exception($"Scaffolding process failed with exit code: {process.ExitCode}. Errors: {toolOutput.JoinToString("\n")}");
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

        var namespaces = new HashSet<string>();

        foreach (var usingDirective in nodes.OfType<UsingDirectiveSyntax>())
        {
            var ns = scaffoldedCode.Substring(usingDirective.Span.Start, usingDirective.Span.Length)
                .Split(' ')[1]
                .TrimEnd(';');

            namespaces.Add(ns);
        }

        var classDeclaration = nodes.OfType<ClassDeclarationSyntax>().Single();
        var classCode = scaffoldedCode.Substring(classDeclaration.Span.Start, classDeclaration.Span.Length);
        var className = classDeclaration.Identifier.ValueText;
        var isDbContext = classCode.Contains(" : DbContext");

        var sourceFile = new ScaffoldedSourceFile(file.FullName, className, classCode, namespaces)
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
