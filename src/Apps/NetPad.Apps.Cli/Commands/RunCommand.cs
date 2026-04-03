using System.CommandLine;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Application.Events;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.ExecutionModel;
using NetPad.ExecutionModel.External;
using NetPad.IO;
using NetPad.Presentation;
using NetPad.Scripts;
using NetPad.Utilities;

namespace NetPad.Apps.Cli.Commands;

enum OutputFormat
{
    Console = 0,
    Text,
    Html,
    HtmlDoc,
    Json
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
        var runCmd = new Command("run", "Run a script or a plain text file.");
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
                "    1. If omitted, or if name matches multiple scripts from your library, you'll be prompted to select from a list.\n" +
                "    2. If omitted and the --eval (-e) option is used you will not be prompted to select a script.",
            Arity = ArgumentArity.ZeroOrOne,
            HelpName = "PATH|NAME"
        };

        var codeOption = new Option<string?>("--eval", "-e")
        {
            Description =
                "The code to execute. Will override the code in the target script, or will be executed as-is if no script was provided.",
            Arity = ArgumentArity.ZeroOrOne,
        };

        var kindOption = new Option<string?>("--kind", "-k")
        {
            Description = "Override the script kind.",
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

        var formatOption = new Option<OutputFormat>("--format", "-f")
        {
            Arity = ArgumentArity.ZeroOrOne,
            HelpName = "console|text|html|htmldoc|json",
            Description =
                "The format of script output. If not specified, will emit structured console output (default).\n" +
                "Values:\n" +
                "    text       Plain text format; useful when piping to a file\n" +
                "    html       HTML fragments\n" +
                "    htmldoc    A complete HTML document\n" +
                "    json       NDJSON (newline-delimited JSON); pipe to jq for filtering",
        };

        var minimalOption = new Option<bool>("--minimal", "-m")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Reduce padding and metadata in output.",
        };

        var noCacheOption = new Option<bool>("--no-cache")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description =
                "Skip the build cache; do not use a cached build, if one exists, and do not cache the build from this run.",
        };

        var forceRebuildOption = new Option<bool>("--rebuild", "-b")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Rebuild even if a cached build exists. Replaces the current cached build, if any.",
        };

        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Emit diagnostic and process logs to stderr.",
        };

        var sqlOption = new Option<bool>("--sql")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Include SQL queries in output. Only applies to --format json.",
        };

        runCmd.Arguments.Add(pathOrNameArg);
        runCmd.Options.Add(codeOption);
        runCmd.Options.Add(kindOption);
        runCmd.Options.Add(sdkOption);
        runCmd.Options.Add(connectionOption);
        runCmd.Options.Add(optimizeOption);
        runCmd.Options.Add(useAspNetOption);
        runCmd.Options.Add(formatOption);
        runCmd.Options.Add(sqlOption);
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

            // Resolve script kind
            var kindStr = p.GetValue(kindOption);
            ScriptKind? scriptKind = null;
            if (kindStr != null)
            {
                if (kindStr.Equals("program", StringComparison.OrdinalIgnoreCase))
                    scriptKind = ScriptKind.Program;
                else if (kindStr.Equals("sql", StringComparison.OrdinalIgnoreCase))
                    scriptKind = ScriptKind.SQL;
                else
                {
                    Presenter.Error($"Unknown script kind '{kindStr}'. Supported values: program, sql.");
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
                scriptKind,
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

            if (options.OutputFormat == OutputFormat.Json)
            {
                options.ScriptArgs.Add("-json");
            }

            if (p.GetValue(minimalOption))
            {
                options.ScriptArgs.Add("-minimal");
            }

            if (p.GetValue(verboseOption))
            {
                options.ScriptArgs.Add("-verbose");
            }

            if (p.GetValue(sqlOption))
            {
                if (options.OutputFormat != OutputFormat.Json)
                {
                    Presenter.Warn("--sql only applies to --format json; ignoring.");
                }
                else
                {
                    options.ScriptArgs.Add("-sql");
                }
            }

            // Forward all unmatched tokens to script
            scriptArgs.AddRange(p.UnmatchedTokens);

            return await ExecuteAsync(options, serviceProvider);
        });
    }

    public static void SetDefaultRunAction(this RootCommand rootCommand)
    {
        rootCommand.SetAction(async p =>
        {
            // Re-invoke with 'run' prepended so the run subcommand handles parsing
            // We don't want attach run symbols to rootCommand directly, otherwise it
            // will leak into every subcommand
            var runArgs = new[] { "run" }.Concat(p.UnmatchedTokens).ToArray();
            return await rootCommand.Parse(runArgs).InvokeAsync();
        });
    }

    private static async Task<int> ExecuteAsync(Options options, IServiceProvider serviceProvider)
    {
        Script? script;

        // If a script path/name is provided, load it (even if -e is also specified,
        // so the script's config and data connection are preserved)
        if (!string.IsNullOrEmpty(options.PathOrName))
        {
            var selectedScriptPath = Helper.SelectScript(serviceProvider, options.PathOrName, "run");
            if (selectedScriptPath == null) return 1;
            script = await Helper.LoadScriptFileAsync(serviceProvider, selectedScriptPath, options.Verbose);
        }
        else if (!string.IsNullOrEmpty(options.Code))
        {
            script = Helper.CreateScriptFromCode(serviceProvider, options.Code);
        }
        else if (Console.IsInputRedirected)
        {
            var stdinCode = await Console.In.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(stdinCode))
            {
                script = Helper.CreateScriptFromCode(serviceProvider, stdinCode);
            }
            else
            {
                Presenter.Error("No input received from stdin.");
                return 1;
            }
        }
        else
        {
            // No path, no code, no stdin — prompt for script selection
            var selectedScriptPath = Helper.SelectScript(serviceProvider, null, "run");
            if (selectedScriptPath == null) return 1;
            script = await Helper.LoadScriptFileAsync(serviceProvider, selectedScriptPath, options.Verbose);
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
        bool htmlOutput = options.OutputFormat is OutputFormat.HtmlDoc or OutputFormat.Html;
        bool jsonOutput = options.OutputFormat == OutputFormat.Json;
        var htmlDocumentOutput = htmlOutput ? new StringBuilder() : null;

        // Create a script runner
        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var scriptRunnerFactory = scope.ServiceProvider.GetRequiredService<IScriptRunnerFactory>();
        var scriptRunner = scriptRunnerFactory.CreateRunner(script);

        // Handle script & runner output
        scriptRunner.AddOutput(new ActionOutputWriter<object>((o, _) =>
        {
            if (o is not ScriptOutput so)
            {
                Console.WriteLine(o);
                return;
            }

            if (htmlDocumentOutput != null && so is { Kind: ScriptOutputKind.Result })
            {
                htmlDocumentOutput.Append(so.Body);
                return;
            }

            if (so is { Kind: ScriptOutputKind.Sql })
            {
                // Do not output
                return;
            }

            // If the script process outputs to STDOUT directly it prints to the console directly.
            // But errors might occur before the script is run, ie: compilation errors. In that
            // case the script runner will emit those errors using this output handler.
            if (!htmlOutput && so is { Kind: ScriptOutputKind.Error })
            {
                if (jsonOutput)
                {
                    var errorLine = System.Text.Json.JsonSerializer.Serialize(
                        new { type = "error", value = so.Body ?? "An error occurred." });
                    Console.WriteLine(errorLine);
                }
                else
                {
                    Presenter.Error(so.Body ?? "An error occurred.");
                }

                return;
            }

            Console.WriteLine(so.Body);
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

        RunResult runResult;

        // Run with status spinner on stderr (suppressed when stderr is redirected or output is JSON)
        if (Console.IsErrorRedirected || jsonOutput)
        {
            runResult = await scriptRunner.RunScriptAsync(runOptions);
        }
        else
        {
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();
            Task<RunResult>? scriptTask = null;

            await Presenter.StatusAsync("Setting up...", async updateStatus =>
            {
                var runningTcs = new TaskCompletionSource();
                var token = eventBus.Subscribe<AppStatusMessagePublishedEvent>(e =>
                {
                    updateStatus(e.Message.Text);
                    if (e.Message.Text is "Running...")
                        runningTcs.TrySetResult();
                    return Task.CompletedTask;
                });

                scriptTask = scriptRunner.RunScriptAsync(runOptions);

                // Spinner stays active during build phases, clears when "Running..." arrives
                // or when script completes (e.g. compilation error)
                await Task.WhenAny(runningTcs.Task, scriptTask);
                eventBus.Unsubscribe(token);
            });

            // Script may still be executing after spinner clears — wait for it
            if (scriptTask is null)
                throw new InvalidOperationException("Script task was not initialized.");
            runResult = await scriptTask;
        }

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

        return MapExitCode(runResult);
    }

    private static int MapExitCode(RunResult result)
    {
        if (result.IsScriptCompletedSuccessfully) return 0;
        if (result.IsRunCancelled) return 130;
        if (!result.IsRunAttemptSuccessful) return 2;
        return 1;
    }
}
