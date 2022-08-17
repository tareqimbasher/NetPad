using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.Configuration;
using NetPad.DotNet;

namespace NetPad.Data.Scaffolding;

public class EntityFrameworkDatabaseScaffolder
{
    private readonly EntityFrameworkDatabaseConnection _connection;
    private readonly ILogger<EntityFrameworkDatabaseScaffolder> _logger;
    private readonly DotNetCSharpProject _project;
    private readonly string _dbModelOutputDirPath;

    public const string DbContextName = "DatabaseContext";

    public EntityFrameworkDatabaseScaffolder(
        EntityFrameworkDatabaseConnection connection,
        Settings settings,
        ILogger<EntityFrameworkDatabaseScaffolder> logger)
    {
        _connection = connection;
        _logger = logger;
        _project = new DotNetCSharpProject(
            Path.Combine(Path.GetTempPath(), AppIdentifier.AppName, "TypedContexts", connection.Id.ToString()),
            projectFileName: "database",
            packageCacheDirectoryPath: Path.Combine(settings.PackageCacheDirectoryPath, "NuGet"));

        _dbModelOutputDirPath = Path.Combine(_project.ProjectDirectoryPath, "DbModel");
    }

    public async Task<bool> ScaffoldAsync()
    {
        await _project.CreateAsync(true);
        await File.WriteAllTextAsync(Path.Combine(_project.ProjectDirectoryPath, "Program.cs"), @"namespace DbScaffolding;

class Program
{
    static void Main()
    {
    }
}");

        await _project.AddPackageAsync(_connection.EntityFrameworkProviderName, null);
        await _project.AddPackageAsync("Microsoft.EntityFrameworkCore.Design", null);

        Directory.CreateDirectory(_dbModelOutputDirPath);

        var args = string.Join(" ", new[]
        {
            "ef dbcontext scaffold",
            _connection.GetConnectionString(),
            _connection.EntityFrameworkProviderName,
            $"--context {DbContextName}",
            $"--namespace TypedContext",
            "--force",
            $"--output-dir {_dbModelOutputDirPath.Replace(_project.ProjectDirectoryPath, "").Trim('/')}" // Relative to proj dir
        });

        _logger.LogDebug("Calling dotnet with args: '{Args}'", args);

        var process = Process.Start(new ProcessStartInfo("dotnet", args)
        {
            UseShellExecute = false,
            WorkingDirectory = _project.ProjectDirectoryPath,
            CreateNoWindow = true
        });

        if (process != null)
        {
            await process.WaitForExitAsync();
            _logger.LogDebug("Call to dotnet scaffold completed with exit code: '{ExitCode}'", process?.ExitCode);
        }

        return process is { ExitCode: 0 };
    }

    public async Task<ScaffoldedDatabaseModel> GetScaffoldedModelAsync()
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
        var sourceFile = new ScaffoldedSourceFile();

        var lines = await File.ReadAllLinesAsync(file.FullName);
        var codeBuilder = new StringBuilder();
        bool classEncountered = false;

        for (var iLine = 0; iLine < lines.Length; iLine++)
        {
            var line = lines[iLine];

            // Handle usings
            if (!classEncountered && line.StartsWith("using ") && line.EndsWith(";"))
            {
                sourceFile.Namespaces.Add(line[("using ".Length - 1)..].TrimEnd(';'));
                continue;
            }

            // Handle namespaces
            if (!classEncountered && line.StartsWith("namespace "))
            {
                codeBuilder.AppendLine(line).AppendLine("{");
                iLine++; // Skip the next line which we already know is the '{' and we already appended
            }

            if (!classEncountered)
            {
                classEncountered = line.Contains(" class ");

                // We've found the line where the class is declared
                if (classEncountered)
                {
                    sourceFile.IsDbContext = line.Contains(": DbContext");
                }

                // Skip any code before the class starts
                if (!classEncountered) continue;
            }

            codeBuilder.AppendLine(line);
        }

        sourceFile.Code = codeBuilder.ToString();
        return sourceFile;
    }
}
