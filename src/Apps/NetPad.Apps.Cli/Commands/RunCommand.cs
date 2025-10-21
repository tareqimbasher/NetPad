using System.CommandLine;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.ExecutionModel;
using NetPad.ExecutionModel.External;
using NetPad.IO;
using NetPad.Presentation;
using NetPad.Scripts;
using NetPad.Utilities;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

enum OutputFormat
{
    Console = 0,
    Text,
    Html,
    HtmlDoc
}

public static class RunCommand
{
    private sealed record Options(
        string? PathOrName,
        string? Code,
        ScriptKind? ScriptKind,
        DotNetFrameworkVersion? SdkVersion,
        DataConnection? DataConnection,
        OptimizationLevel? OptimizationLevel,
        bool? UseAspNet,
        bool NoCache,
        bool ForceRebuild,
        bool Verbose,
        List<string> ScriptArgs,
        OutputFormat OutputFormat)
    {
        public bool NoCache { get; set; } = NoCache;
    }

    public static void AddRunCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var runCmd = new Command(
            "run",
            "Run a script or a plain text file.");
        parent.Subcommands.Add(runCmd);

        var pathOrNameArg = new Argument<string>("PATH|NAME")
        {
            Description =
                "A path to a script or text file, or a name (or partial name) to search for in your script library.\n" +
                "Examples:\n" +
                "    run /path/to/myscript.netpad   <= absolute path to a .netpad script\n" +
                "    run ./myscript.netpad          <= relative path to a .netpad script\n" +
                "    run /path/to/myscript.cs       <= path to a text file that contains code to be executed\n" +
                "    run myscript                   <= looks for a script in your library with a path containing the word 'myscript' \n" +
                "Notes:\n" +
                "    1. If omitted, or if name matches multiple scripts from your library, you’ll be prompted to select from a list.\n" +
                "    2. If omitted and the --code (-x) option is used you will not be prompted to select a script.",
            Arity = ArgumentArity.ZeroOrOne,
            HelpName = "PATH|NAME"
        };

        var codeOption = new Option<string?>("--code", "-x")
        {
            Description =
                "The code to execute. Will override the code in the target script, or will be executed it as-is if no script was provided.",
            Arity = ArgumentArity.ZeroOrOne,
        };

        var sdkOption = new Option<int?>("--sdk")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "The .NET SDK major version to use.",
            HelpName = string.Join("|", Enumerable.Range(
                DotNetFrameworkVersionUtil.MinSupportedDotNetVersion,
                DotNetFrameworkVersionUtil.MaxSupportedDotNetVersion + 1 -
                DotNetFrameworkVersionUtil.MinSupportedDotNetVersion))
        };

        var connectionOption = new Option<string?>("--connection")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "The name of the database connection to use.",
            HelpName = "name"
        };

        var optimizeOption = new Option<bool?>("--optimize")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Enable compiler optimizations."
        };

        var useAspNetOption = new Option<bool?>("--aspnet")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Reference ASP.NET assemblies."
        };

        var formatOption = new Option<OutputFormat>("--format")
        {
            Arity = ArgumentArity.ZeroOrOne,
            HelpName = "text|html|htmldoc",
            Description =
                "The format of script output. If not specified, will emit structured console output (default).\n" +
                "Values:\n" +
                "    text       Plain text format; useful when piping to a file\n" +
                "    html       HTML fragments\n" +
                "    htmldoc    A complete HTML document",
        };

        var minimalOption = new Option<bool>("--minimal")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "If possible, use more minimal output formatting.",
        };

        var noCacheOption = new Option<bool>("--no-cache")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description =
                "Skip the build cache; do not use a cached build, if one exists, and do not cache the build from this run.",
        };

        var forceRebuildOption = new Option<bool>("--rebuild")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Rebuild even if a cached build exists. Replaces the current cached build, if any.",
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Be verbose.",
        };

        runCmd.Arguments.Add(pathOrNameArg);
        runCmd.Options.Add(codeOption);
        runCmd.Options.Add(sdkOption);
        runCmd.Options.Add(connectionOption);
        runCmd.Options.Add(optimizeOption);
        runCmd.Options.Add(useAspNetOption);
        runCmd.Options.Add(formatOption);
        runCmd.Options.Add(minimalOption);
        runCmd.Options.Add(noCacheOption);
        runCmd.Options.Add(forceRebuildOption);
        runCmd.Options.Add(verboseOption);
        runCmd.SetAction(async p =>
        {
            // Resolve the target connection
            var connectionName = p.GetValue(connectionOption);
            DataConnection? connection = null;
            if (!string.IsNullOrWhiteSpace(connectionName))
            {
                connection = await Helper.GetConnectionByNameAsync(serviceProvider, connectionName);
                if (connection == null)
                {
                    return 1;
                }
            }

            var sdkMajor = p.GetValue(sdkOption);
            DotNetFrameworkVersion? sdkVersion =
                sdkMajor == null ? null : DotNetFrameworkVersionUtil.GetFrameworkVersion(sdkMajor.Value);

            OptimizationLevel? optimizationLevel = p.GetValue(optimizeOption) switch
            {
                null => null,
                true => OptimizationLevel.Release,
                _ => OptimizationLevel.Debug
            };

            var scriptArgs = new List<string>();

            var options = new Options(
                p.GetValue(pathOrNameArg),
                p.GetValue(codeOption),
                ScriptKind.Program,
                sdkVersion,
                connection,
                optimizationLevel,
                p.GetValue(useAspNetOption),
                p.GetValue(noCacheOption),
                p.GetValue(forceRebuildOption),
                p.GetValue(verboseOption),
                scriptArgs,
                p.GetValue(formatOption)
            );

            // Validate options
            if (options.NoCache && options.ForceRebuild)
            {
                Presenter.Error($"Cannot use {noCacheOption.Name} and {forceRebuildOption.Name} at the same time.");
                return 1;
            }

            // Forward some options to script
            if (options.OutputFormat == OutputFormat.Text)
            {
                options.ScriptArgs.Add("-text");
            }

            if (options.OutputFormat is OutputFormat.HtmlDoc or OutputFormat.Html)
            {
                options.ScriptArgs.Add("-html-msg");
            }

            if (p.GetValue(minimalOption))
            {
                options.ScriptArgs.Add("-minimal");
            }

            if (p.GetValue(verboseOption))
            {
                options.ScriptArgs.Add("-verbose");
            }

            // Forward all unmatched tokens to script
            scriptArgs.AddRange(p.UnmatchedTokens);

            return await ExecuteAsync(options, serviceProvider);
        });
    }

    private static async Task<int> ExecuteAsync(Options options, IServiceProvider serviceProvider)
    {
        Script? script;

        if (string.IsNullOrEmpty(options.Code))
        {
            var selectedScriptPath = Helper.SelectScript(serviceProvider, options.PathOrName);
            if (selectedScriptPath == null) return 1;
            script = await Helper.LoadScriptFileAsync(serviceProvider, selectedScriptPath, options.Verbose);
        }
        else
        {
            script = Helper.CreateScriptFromCode(serviceProvider, options.Code);
        }

        if (script == null) return 1;

        ApplyOptions(script, options);

        return await RunScriptAsync(serviceProvider, script, options);
    }

    private static void ApplyOptions(Script script, Options options)
    {
        if (!string.IsNullOrEmpty(options.Code))
        {
            script.UpdateCode(options.Code);
        }

        if (options.ScriptKind.HasValue) script.Config.SetKind(options.ScriptKind.Value);
        if (options.OptimizationLevel.HasValue) script.Config.SetOptimizationLevel(options.OptimizationLevel.Value);
        if (options.UseAspNet.HasValue) script.Config.SetUseAspNet(options.UseAspNet.Value);

        if (options.SdkVersion != null)
        {
            script.Config.SetTargetFrameworkVersion(options.SdkVersion.Value);
        }

        if (options.DataConnection != null)
        {
            script.SetDataConnection(options.DataConnection);
        }
    }

    private static async Task<int> RunScriptAsync(IServiceProvider serviceProvider, Script script, Options options)
    {
        if (options.Verbose) Presenter.Info("Setting up...");

        bool htmlOutput = options.OutputFormat is OutputFormat.HtmlDoc or OutputFormat.Html;
        var htmlDocumentOutput = htmlOutput ? new StringBuilder() : null;

        // Create a script runner
        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var scriptRunnerFactory = scope.ServiceProvider.GetRequiredService<IScriptRunnerFactory>();
        var scriptRunner = scriptRunnerFactory.CreateRunner(script);

        // Handle script & runner output
        scriptRunner.AddOutput(new ActionOutputWriter<object>((o, _) =>
        {
            if (htmlDocumentOutput != null && o is HtmlResultsScriptOutput htmlScriptOutput)
            {
                htmlDocumentOutput.Append(htmlScriptOutput.Body);
                return;
            }

            if (o is HtmlSqlScriptOutput)
            {
                // Do not output
                return;
            }

            // If the script process outputs to STDOUT directly it prints to the console directly.
            // But errors might occur before the script is run, ie: compilation errors. In that
            // case the script runner will emit those errors using this output handler.
            if (!htmlOutput && o is ScriptOutput error)
            {
                Presenter.Error(error.Body?.ToString() ?? "An error occured.");
                return;
            }

            if (o is ScriptOutput scriptOutput)
            {
                Console.WriteLine(scriptOutput.Body);
            }

            Console.WriteLine(o);
        }));

        // Configure run options
        var runOptions = new RunOptions();
        runOptions.SetOption(new ExternalScriptRunnerOptions
        {
            NoCache = options.NoCache,
            ForceRebuild = options.ForceRebuild,
            ProcessCliArgs = options.ScriptArgs.ToArray(),
            RedirectIo = htmlOutput
        });

        await scriptRunner.RunScriptAsync(runOptions);

        if (htmlDocumentOutput != null)
        {
            string html;

            if (options.OutputFormat == OutputFormat.HtmlDoc)
            {
                var styles = AssemblyUtil.ReadEmbeddedResource(typeof(RunCommand).Assembly, "Assets.styles.css");
                html = $"""
                        <!DOCTYPE html>
                        <html>
                        <head>
                          <meta charset="utf-8" />
                          <meta name="viewport" content="width=device-width" />
                          <style>{styles}</style>
                        </head>
                        <body>
                        {htmlDocumentOutput}
                        </body>
                        </html>
                        """;
            }
            else
            {
                html = htmlDocumentOutput.ToString();
            }

            Console.WriteLine(html);
        }

        return 0;
    }
}
