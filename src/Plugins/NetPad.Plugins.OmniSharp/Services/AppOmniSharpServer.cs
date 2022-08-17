using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Events;
using NetPad.Plugins.OmniSharp.Events;
using NetPad.Scripts;
using NetPad.Utilities;
using OmniSharp;
using OmniSharp.FileWatching;
using OmniSharp.Models.FilesChanged;
using OmniSharp.Models.UpdateBuffer;
using OmniSharp.Stdio;

namespace NetPad.Plugins.OmniSharp.Services;

/// <summary>
/// A wrapper around an <see cref="IOmniSharpServer"/> that includes app-specific functionality.
/// </summary>
public class AppOmniSharpServer
{
    private readonly ScriptEnvironment _environment;
    private readonly IOmniSharpServerFactory _omniSharpServerFactory;
    private readonly IOmniSharpServerLocator _omniSharpServerLocator;
    private readonly Settings _settings;
    private readonly ICodeParser _codeParser;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly List<EventSubscriptionToken> _subscriptionTokens;

    private IOmniSharpStdioServer? _omniSharpServer;
    private readonly ScriptProject _project;

    public AppOmniSharpServer(
        ScriptEnvironment environment,
        IOmniSharpServerFactory omniSharpServerFactory,
        IOmniSharpServerLocator omniSharpServerLocator,
        Settings settings,
        ICodeParser codeParser,
        IEventBus eventBus,
        ILogger<AppOmniSharpServer> logger,
        // ReSharper disable once ContextualLoggerProblem
        ILogger<ScriptProject> scriptProjectLogger)
    {
        _environment = environment;
        _omniSharpServerFactory = omniSharpServerFactory;
        _omniSharpServerLocator = omniSharpServerLocator;
        _settings = settings;
        _codeParser = codeParser;
        _eventBus = eventBus;
        _logger = logger;
        _subscriptionTokens = new();

        _project = new ScriptProject(environment.Script, settings, scriptProjectLogger);
    }

    public Guid ScriptId => _environment.Script.Id;

    public ScriptProject Project => _project;

    public IOmniSharpStdioServer OmniSharpServer => _omniSharpServer
                                               ?? throw new InvalidOperationException(
                                                   $"OmniSharp server has not been started yet. Script ID: {_environment.Script.Id}");

    /// <summary>
    /// Starts the server.
    /// </summary>
    public async Task<bool> StartAsync()
    {
        var omnisharpServerLocation = await _omniSharpServerLocator.GetServerLocationAsync();
        var executablePath = omnisharpServerLocation?.ExecutablePath;

        if (!IsValidServerExecutablePath(executablePath))
        {
            return false;
        }

        _logger.LogDebug("Initializing script project for script: {Script}", _environment.Script);
        await _project.CreateAsync();

        var codeChangeToken = _eventBus.Subscribe<ScriptCodeUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != _environment.Script.Id || _omniSharpServer == null)
            {
                return;
            }

            await UpdateOmniSharpCodeBufferAsync();

            // Technically we should be doing this, instead of the previous line however when
            // we do, code in bootstrapper program like the .Dump() extension method is not recognized
            // TODO need to find a point where we can determine OmniSharp has fully started and is ready to update buffer and for it to register
            // var parsingResult = _codeParser.Parse(_environment.Script);
            // await UpdateOmniSharpCodeBufferWithUserProgramAsync(parsingResult);
        });

        _subscriptionTokens.Add(codeChangeToken);

        var namespacesChangeToken = _eventBus.Subscribe<ScriptNamespacesUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != _environment.Script.Id || _omniSharpServer == null)
            {
                return;
            }

            var parsingResult = _codeParser.Parse(_environment.Script);
            await UpdateOmniSharpCodeBufferWithBootstrapperProgramAsync(parsingResult);
        });

        _subscriptionTokens.Add(namespacesChangeToken);

        var referencesChangeToken = _eventBus.Subscribe<ScriptReferencesUpdatedEvent>(async ev =>
        {
            if (!ev.Added.Any() && !ev.Removed.Any())
            {
                return;
            }

            foreach (var addedReference in ev.Added)
            {
                if (addedReference is PackageReference pkgRef)
                {
                    try
                    {
                        await _project.AddPackageAsync(pkgRef.PackageId, pkgRef.Version);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to add package to project. " +
                                             "Package ID: {PackageId}. Package version: {PackageVersion}",
                            pkgRef.PackageId,
                            pkgRef.Version);
                    }
                }
                else if (addedReference is AssemblyReference asmRef)
                {
                    try
                    {
                        await _project.AddAssemblyReferenceAsync(asmRef.AssemblyPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to add assembly reference to project. " +
                                             "Assembly path: {AssemblyPath}", asmRef.AssemblyPath);
                    }
                }
            }

            foreach (var removedReference in ev.Removed)
            {
                if (removedReference is PackageReference pkgRef)
                {
                    try
                    {
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to remove package from project. " +
                                             "Package ID: {PackageId}. Package version: {PackageVersion}",
                            pkgRef.PackageId,
                            pkgRef.Version);
                    }
                }
                else if (removedReference is AssemblyReference asmRef)
                {
                    try
                    {
                        await _project.RemoveAssemblyReferenceAsync(asmRef.AssemblyPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to remove assembly reference from project. " +
                                             "Assembly path: {AssemblyPath}", asmRef.AssemblyPath);
                    }
                }
            }

            await OmniSharpServer.SendAsync(new[]
            {
                new FilesChangedRequest()
                {
                    FileName = _project.ProjectFilePath,
                    ChangeType = FileChangeType.Change
                }
            });
        });

        _subscriptionTokens.Add(referencesChangeToken);

        await StartOmniSharpServerAsync(executablePath!);

        await _eventBus.PublishAsync(new OmniSharpServerStartedEvent(this));

        return true;
    }

    /// <summary>
    /// Stops the server.
    /// </summary>
    public async Task StopAsync()
    {
        foreach (var token in _subscriptionTokens)
        {
            _eventBus.Unsubscribe(token);
        }

        await StopOmniSharpServerAsync();

        await _eventBus.PublishAsync(new OmniSharpServerStoppedEvent(this));

        await _project.DeleteAsync();
    }

    public async Task<bool> RestartAsync(Action<string>? progress = null)
    {
        progress?.Invoke("Stopping OmniSharp server...");
        await StopOmniSharpServerAsync();

        var omnisharpServerLocation = await _omniSharpServerLocator.GetServerLocationAsync();
        var executablePath = omnisharpServerLocation?.ExecutablePath;

        if (!IsValidServerExecutablePath(executablePath))
        {
            return false;
        }

        progress?.Invoke("Starting OmniSharp server...");
        await StartOmniSharpServerAsync(executablePath!);

        await _eventBus.PublishAsync(new OmniSharpServerRestartedEvent(this));

        return true;
    }

    private async Task StartOmniSharpServerAsync(string executablePath)
    {
        string args = new[]
        {
            $"--hostPID {Environment.ProcessId}",
            "--encoding utf-8",
            "--loglevel Information",
            //"-z",

            "FileOptions:SystemExcludeSearchPatterns:0=**/.git",
            "FileOptions:SystemExcludeSearchPatterns:1=**/.svn",
            "FileOptions:SystemExcludeSearchPatterns:2=**/.hg",
            "FileOptions:SystemExcludeSearchPatterns:3=**/CVS",
            "FileOptions:SystemExcludeSearchPatterns:4=**/.DS_Store",
            "FileOptions:SystemExcludeSearchPatterns:5=**/Thumbs.db",
            $"RoslynExtensionsOptions:EnableAnalyzersSupport={_settings.OmniSharp.EnableAnalyzersSupport}",
            "RoslynExtensionsOptions:EnableEditorConfigSupport=false",
            "RoslynExtensionsOptions:EnableDecompilationSupport=true",
            $"RoslynExtensionsOptions:EnableImportCompletion={_settings.OmniSharp.EnableImportCompletion}",
            "RoslynExtensionsOptions:EnableAsyncCompletion=false",

            $"RoslynExtensionsOptions:InlayHintsOptions:EnableForParameters={_settings.OmniSharp.InlayHints.EnableParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForLiteralParameters={_settings.OmniSharp.InlayHints.EnableLiteralParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForIndexerParameters={_settings.OmniSharp.InlayHints.EnableIndexerParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForObjectCreationParameters={_settings.OmniSharp.InlayHints.EnableObjectCreationParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForOtherParameters={_settings.OmniSharp.InlayHints.EnableOtherParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:SuppressForParametersThatDifferOnlyBySuffix={_settings.OmniSharp.InlayHints.SuppressForParametersThatDifferOnlyBySuffix}",
            $"RoslynExtensionsOptions:InlayHintsOptions:SuppressForParametersThatMatchMethodIntent={_settings.OmniSharp.InlayHints.SuppressForParametersThatMatchMethodIntent}",
            $"RoslynExtensionsOptions:InlayHintsOptions:SuppressForParametersThatMatchArgumentName={_settings.OmniSharp.InlayHints.SuppressForParametersThatMatchArgumentName}",
            $"RoslynExtensionsOptions:InlayHintsOptions:EnableForTypes={_settings.OmniSharp.InlayHints.EnableTypes}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForImplicitVariableTypes={_settings.OmniSharp.InlayHints.EnableImplicitVariableTypes}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForLambdaParameterTypes={_settings.OmniSharp.InlayHints.EnableLambdaParameterTypes}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForImplicitObjectCreation={_settings.OmniSharp.InlayHints.EnableImplicitObjectCreation}",
        }.JoinToString(" ");

        var omniSharpServer = _omniSharpServerFactory.CreateStdioServerFromNewProcess(executablePath, _project.ProjectDirectoryPath, args);

        _logger.LogDebug("Starting omnisharp server from path: {OmniSharpExePath} with args: {Args} and project dir: {ProjDirPath}",
            executablePath,
            args,
            _project.ProjectDirectoryPath);

        await omniSharpServer.StartAsync();

        _omniSharpServer = omniSharpServer;

        // It takes some time for OmniSharp to register its updated buffer after it starts
        if (!string.IsNullOrWhiteSpace(_environment.Script.Code))
        {
            Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    await UpdateOmniSharpCodeBufferAsync();
                    await Task.Delay(500);
                }
            });
        }
    }

    private async Task StopOmniSharpServerAsync()
    {
        if (_omniSharpServer == null)
        {
            return;
        }

        await _omniSharpServer.StopAsync();

        _omniSharpServer = null;
    }

    private bool IsValidServerExecutablePath(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            _logger.LogError($"Could not locate the OmniSharp Server executable. OmniSharp functionality will be disabled.");
            return false;
        }

        if (!File.Exists(executablePath))
        {
            _logger.LogError("OmniSharp executable path does not exist at: {OmniSharpExecutablePath}. " +
                             "OmniSharp functionality will not be enabled", executablePath);
            return false;
        }

        return true;
    }

    public async Task UpdateOmniSharpCodeBufferAsync()
    {
        var parsingResult = _codeParser.Parse(_environment.Script);
        await UpdateOmniSharpCodeBufferWithBootstrapperProgramAsync(parsingResult);
        await UpdateOmniSharpCodeBufferWithUserProgramAsync(parsingResult);
    }

    private async Task UpdateOmniSharpCodeBufferWithUserProgramAsync(CodeParsingResult parsingResult)
    {
        await OmniSharpServer.SendAsync(new UpdateBufferRequest
        {
            FileName = _project.UserProgramFilePath,
            Buffer = parsingResult.UserProgram
        });
    }

    private async Task UpdateOmniSharpCodeBufferWithBootstrapperProgramAsync(CodeParsingResult parsingResult)
    {
        var namespaces = string.Join("\n", parsingResult.Namespaces.Select(ns => $"global using {ns};"));
        var bootstrapperProgramCode = $"{namespaces}\n\n{parsingResult.BootstrapperProgram}";

        await OmniSharpServer.SendAsync(new UpdateBufferRequest
        {
            FileName = _project.BootstrapperProgramFilePath,
            Buffer = bootstrapperProgramCode
        });
    }
}
