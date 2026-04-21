using System.CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Apps.Scripts;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Scripts;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

public static class NewCommand
{
    public static void AddNewCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var newCmd = new Command("new", "Create a new script.");

        parent.Subcommands.Add(newCmd);

        var nameArg = new Argument<string>("name")
        {
            Description =
                "The name of the script to create. Can include a path.\n" +
                "Examples:\n" +
                "    new myscript            <= creates myscript.netpad in the current directory\n" +
                "    new myscript.cs         <= creates a plain .cs file in the current directory\n" +
                "    new ./scripts/myscript  <= relative path from the current directory\n" +
                "    new myscript -l         <= creates in your script library",
        };

        var libraryOption = new Option<bool>("--library", "-l")
        {
            Description = "Create the script in your script library instead of the current directory.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var kindOption = new Option<string?>("--kind", "-k")
        {
            Description = "The type of script to create.",
            Arity = ArgumentArity.ZeroOrOne,
            HelpName = "program|sql"
        };

        var sdkOption = new Option<int?>("--sdk", "-s")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "The .NET SDK major version to use.",
            HelpName = string.Join("|", Enumerable.Range(
                DotNetFrameworkVersionUtil.MinSupportedDotNetVersion,
                DotNetFrameworkVersionUtil.MaxSupportedDotNetVersion + 1 -
                DotNetFrameworkVersionUtil.MinSupportedDotNetVersion))
        };

        var connectionOption = new Option<string?>("--connection", "-c")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "The name of the database connection to use.",
            HelpName = "name"
        };

        var optimizeOption = new Option<bool?>("--optimize", "-O")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Enable compiler optimizations."
        };

        var useAspNetOption = new Option<bool?>("--aspnet")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Reference ASP.NET assemblies."
        };

        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Overwrite the file if it already exists without prompting.",
            Arity = ArgumentArity.ZeroOrOne
        };

        newCmd.Arguments.Add(nameArg);
        newCmd.Options.Add(libraryOption);
        newCmd.Options.Add(kindOption);
        newCmd.Options.Add(sdkOption);
        newCmd.Options.Add(connectionOption);
        newCmd.Options.Add(optimizeOption);
        newCmd.Options.Add(useAspNetOption);
        newCmd.Options.Add(forceOption);

        newCmd.SetAction(async p =>
        {
            var name = p.GetValue(nameArg)!;
            var library = p.GetValue(libraryOption);
            var kindStr = p.GetValue(kindOption);
            var force = p.GetValue(forceOption);

            if (!Helper.TryParseScriptKind(kindStr, out var parsedKind))
            {
                return 1;
            }

            var kind = parsedKind ?? ScriptKind.Program;

            // Determine if this is a plain code file (extensions NetPad recognizes as script sources)
            bool isPlainCodeFile =
                name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".csx", StringComparison.OrdinalIgnoreCase);

            if (isPlainCodeFile && kind == ScriptKind.SQL)
            {
                Presenter.Error(
                    "Cannot create a SQL script with a .cs or .csx extension. " +
                    "Drop the extension to create a .netpad file.");
                return 1;
            }

            // Resolve the output file path
            var filePath = ResolvePath(serviceProvider, name, library, isPlainCodeFile);
            if (filePath == null) return 1;

            // Handle file collision
            if (File.Exists(filePath))
            {
                var resolvedPath = HandleCollision(filePath, force);
                if (resolvedPath == null) return 0; // User cancelled
                filePath = resolvedPath;
            }

            if (isPlainCodeFile)
            {
                return CreatePlainCodeFile(filePath);
            }

            return await CreateNetpadScript(serviceProvider, filePath, kind, p, sdkOption, connectionOption,
                optimizeOption, useAspNetOption);
        });
    }

    private static string? ResolvePath(
        IServiceProvider serviceProvider,
        string name,
        bool library,
        bool isPlainCodeFile)
    {
        string basePath;

        if (library)
        {
            var settings = serviceProvider.GetRequiredService<Settings>();
            basePath = settings.ScriptsDirectoryPath;
        }
        else
        {
            basePath = Directory.GetCurrentDirectory();
        }

        // If the name is an absolute path, use it directly (ignore -l)
        string filePath;
        if (Path.IsPathRooted(name))
        {
            filePath = name;
        }
        else
        {
            filePath = Path.Combine(basePath, name);
        }

        // Ensure correct extension
        if (!isPlainCodeFile && !filePath.EndsWith(Script.STANDARD_EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            filePath += Script.STANDARD_EXTENSION;
        }

        filePath = Path.GetFullPath(filePath);

        // Ensure parent directory exists
        var dir = Path.GetDirectoryName(filePath);
        if (dir != null)
        {
            Directory.CreateDirectory(dir);
        }

        return filePath;
    }

    private static string? HandleCollision(string filePath, bool force)
    {
        if (force) return filePath;

        var fileName = Path.GetFileName(filePath);
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[yellow]'{fileName}' already exists.[/] What would you like to do?")
                .AddChoices("Overwrite", "Create with new name", "Cancel"));

        return choice switch
        {
            "Overwrite" => filePath,
            "Create with new name" => GenerateUniquePath(filePath),
            _ => null
        };
    }

    private static string GenerateUniquePath(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath)!;
        var ext = Path.GetExtension(filePath);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

        int suffix = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{nameWithoutExt}_{suffix}{ext}");
            suffix++;
        } while (File.Exists(candidate));

        return candidate;
    }

    private static int CreatePlainCodeFile(string filePath)
    {
        File.WriteAllText(filePath, "Console.WriteLine(\"Hello World\");\n");
        AnsiConsole.MarkupLineInterpolated($"[green]success:[/] {filePath}");
        return 0;
    }

    private static async Task<int> CreateNetpadScript(
        IServiceProvider serviceProvider,
        string filePath,
        ScriptKind kind,
        ParseResult parseResult,
        Option<int?> sdkOption,
        Option<string?> connectionOption,
        Option<bool?> optimizeOption,
        Option<bool?> useAspNetOption)
    {
        // Resolve SDK version
        var sdkMajor = parseResult.GetValue(sdkOption);
        var dotNetInfo = serviceProvider.GetRequiredService<IDotNetInfo>();
        DotNetFrameworkVersion sdkVersion;

        if (sdkMajor != null)
        {
            sdkVersion = DotNetFrameworkVersionUtil.GetFrameworkVersion(sdkMajor.Value);
        }
        else
        {
            var latest = dotNetInfo.GetLatestSupportedDotNetSdkVersion()?.GetFrameworkVersion();
            if (latest == null)
            {
                Presenter.Error("Could not find an installed .NET SDK.");
                return 1;
            }

            sdkVersion = latest.Value;
        }

        // Resolve optimization level
        OptimizationLevel optimizationLevel = parseResult.GetValue(optimizeOption) switch
        {
            true => OptimizationLevel.Release,
            _ => OptimizationLevel.Debug
        };

        // Resolve data connection
        DataConnection? connection = null;
        var connectionName = parseResult.GetValue(connectionOption);
        if (!string.IsNullOrWhiteSpace(connectionName))
        {
            connection = await Helper.GetConnectionByNameAsync(serviceProvider, connectionName);
            if (connection == null) return 1;
        }

        // Resolve ASP.NET
        bool useAspNet = parseResult.GetValue(useAspNetOption) == true;

        // Build the script
        var code = kind == ScriptKind.SQL ? string.Empty : "Console.WriteLine(\"Hello World\");\n";

        var script = new Script(
            ScriptIdGenerator.NewId(),
            Path.GetFileNameWithoutExtension(filePath),
            new ScriptConfig(
                kind,
                sdkVersion,
                namespaces: ScriptConfigDefaults.DefaultNamespaces,
                optimizationLevel: optimizationLevel,
                useAspNet: useAspNet
            ),
            code
        );

        script.SetPath(filePath);

        if (connection != null)
        {
            script.SetDataConnection(connection);
        }

        // Save
        await File.WriteAllTextAsync(filePath, ScriptSerializer.Serialize(script));
        AnsiConsole.MarkupLineInterpolated($"[green]success:[/] {filePath}");
        return 0;
    }
}
