using System.Collections.Concurrent;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
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
    private readonly IDataConnectionResourcesCache _dataConnectionResourcesCache;

    private readonly Settings _settings;
    private readonly ICodeParser _codeParser;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly List<EventSubscriptionToken> _subscriptionTokens;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _bufferUpdateSemaphores = new();

    private IOmniSharpStdioServer? _omniSharpServer;

    public AppOmniSharpServer(
        ScriptEnvironment environment,
        IOmniSharpServerFactory omniSharpServerFactory,
        IOmniSharpServerLocator omniSharpServerLocator,
        IDataConnectionResourcesCache dataConnectionResourcesCache,
        Settings settings,
        ICodeParser codeParser,
        IEventBus eventBus,
        IDotNetInfo dotNetInfo,
        ILogger<AppOmniSharpServer> logger,
        // ReSharper disable once ContextualLoggerProblem
        ILogger<ScriptProject> scriptProjectLogger)
    {
        _environment = environment;
        _omniSharpServerFactory = omniSharpServerFactory;
        _omniSharpServerLocator = omniSharpServerLocator;
        _dataConnectionResourcesCache = dataConnectionResourcesCache;
        _settings = settings;
        _codeParser = codeParser;
        _eventBus = eventBus;
        _logger = logger;
        _subscriptionTokens = new List<EventSubscriptionToken>();

        Project = new ScriptProject(environment.Script, dotNetInfo, settings, scriptProjectLogger);
    }

    public Guid ScriptId => _environment.Script.Id;

    public ScriptProject Project { get; }

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
        await Project.CreateAsync(ProjectOutputType.Executable, true);
        await Project.RestoreAsync();

        InitializeEventHandlers();

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

        var semaphores = _bufferUpdateSemaphores.Values.ToArray();
        foreach (var semaphore in semaphores)
        {
            semaphore.Dispose();
        }

        await _eventBus.PublishAsync(new OmniSharpServerStoppedEvent(this));

        await Project.DeleteAsync();
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
            $"RoslynExtensionsOptions:InlayHintsOptions:ForImplicitObjectCreation={_settings.OmniSharp.InlayHints.EnableImplicitObjectCreation}"
        }.JoinToString(" ");

        var omniSharpServer = _omniSharpServerFactory.CreateStdioServerFromNewProcess(executablePath, Project.ProjectDirectoryPath, args);

        _logger.LogDebug("Starting omnisharp server from path: {OmniSharpExePath} with args: {Args} and project dir: {ProjDirPath}",
            executablePath,
            args,
            Project.ProjectDirectoryPath);

        await omniSharpServer.StartAsync();

        _omniSharpServer = omniSharpServer;

        // It takes some time for OmniSharp to register its updated buffer after it starts
        if (!string.IsNullOrWhiteSpace(_environment.Script.Code))
        {
            // We don't want to await
#pragma warning disable CS4014
            Task.Run(async () =>
#pragma warning restore CS4014
            {
                await Task.Delay(3000);

                bool shouldUpdateDataConnectionCodeBuffer = _environment.Script.DataConnection == null
                                                            || _dataConnectionResourcesCache.HasCachedResources(_environment.Script.DataConnection.Id);
                if (shouldUpdateDataConnectionCodeBuffer)
                    await UpdateOmniSharpCodeBufferWithDataConnectionAsync(_environment.Script.DataConnection);
                else
                    await UpdateOmniSharpCodeBufferAsync();
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
            _logger.LogError("Could not locate the OmniSharp Server executable. OmniSharp functionality will be disabled");
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


    # region Event Handlers

    private void InitializeEventHandlers()
    {
        Subscribe<ScriptCodeUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != _environment.Script.Id) return;

            await UpdateOmniSharpCodeBufferAsync();

            // Technically we should be doing this, instead of the previous line however when
            // we do, code in bootstrapper program like the .Dump() extension method is not recognized
            // TODO need to find a point where we can determine OmniSharp has fully started and is ready to update buffer and for it to register
            // var parsingResult = _codeParser.Parse(_environment.Script);
            // await UpdateOmniSharpCodeBufferWithUserProgramAsync(parsingResult);
        });

        Subscribe<ScriptNamespacesUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != _environment.Script.Id) return;

            var script = _environment.Script;
            var parsingResult = _codeParser.Parse(script.Code, script.Config.Kind, script.Config.Namespaces);
            await UpdateOmniSharpCodeBufferWithBootstrapperProgramAsync(parsingResult);
        });

        Subscribe<ScriptReferencesUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != _environment.Script.Id) return;

            if (!ev.Added.Any() && !ev.Removed.Any()) return;

            await Project.AddReferencesAsync(ev.Added);

            await Project.RemoveReferencesAsync(ev.Removed);

            await NotifyOmniSharpServerProjectFileChangedAsync();
        });

        Subscribe<DataConnectionResourcesUpdatedEvent>(ev =>
        {
            if (_environment.Script.DataConnection == null || ev.DataConnection.Id != _environment.Script.DataConnection.Id) return Task.CompletedTask;

            var dataConnection = ev.DataConnection;

            Task.Run(async () => { await UpdateOmniSharpCodeBufferWithDataConnectionAsync(dataConnection); });

            return Task.CompletedTask;
        });

        Subscribe<ScriptDataConnectionChangedEvent>(ev =>
        {
            if (ev.Script.Id != _environment.Script.Id) return Task.CompletedTask;

            var dataConnection = ev.DataConnection;

            Task.Run(async () => { await UpdateOmniSharpCodeBufferWithDataConnectionAsync(dataConnection); });

            return Task.CompletedTask;
        });
    }

    private void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class, IEvent
    {
        var token = _eventBus.Subscribe<TEvent>(ev =>
        {
            if (_omniSharpServer != null)
            {
                // We don't want to await OmniSharp event handlers
                handler(ev);
            }

            return Task.CompletedTask;
        });
        _subscriptionTokens.Add(token);
    }

    #endregion

    public async Task UpdateOmniSharpCodeBufferAsync()
    {
        var script = _environment.Script;
        var parsingResult = _codeParser.Parse(script.Code, script.Config.Kind, script.Config.Namespaces);
        await UpdateOmniSharpCodeBufferWithBootstrapperProgramAsync(parsingResult);
        await UpdateOmniSharpCodeBufferWithUserProgramAsync(parsingResult);
    }

    private async Task UpdateOmniSharpCodeBufferWithUserProgramAsync(CodeParsingResult parsingResult)
    {
        await UpdateBufferAsync(Project.UserProgramFilePath, parsingResult.UserProgram.Code.Value + ";");
    }

    private async Task UpdateOmniSharpCodeBufferWithBootstrapperProgramAsync(CodeParsingResult parsingResult)
    {
        var usings = parsingResult.CombineSourceCode().GetAllUsings()
            .Select(u => u.ToCodeString(true))
            .JoinToString(Environment.NewLine);

        var bootstrapperProgramCode = $"{usings}\n\n{parsingResult.BootstrapperProgram.Code.ToCodeString()}";

        await UpdateBufferAsync(Project.BootstrapperProgramFilePath, bootstrapperProgramCode);
    }

    private async Task UpdateOmniSharpCodeBufferWithDataConnectionAsync(DataConnection? dataConnection)
    {
        List<Reference> references = new List<Reference>();

        if (dataConnection != null)
        {
            references.AddRange(await _dataConnectionResourcesCache.GetRequiredReferencesAsync(dataConnection));

            var assembly = await _dataConnectionResourcesCache.GetAssemblyAsync(dataConnection);
            if (assembly != null)
                references.Add(new AssemblyImageReference(assembly));
        }

        await Project.UpdateReferencesFromDataConnectionAsync(dataConnection, references);
        await NotifyOmniSharpServerProjectFileChangedAsync();

        var sourceCode = dataConnection == null
            ? null
            : _dataConnectionResourcesCache.GetSourceGeneratedCodeAsync(dataConnection);
        await UpdateOmniSharpCodeBufferWithDataConnectionProgramAsync(sourceCode);

        // Needed to trigger diagnostics and semantic highlighting for script file
        await Task.Delay(1000);
        await UpdateOmniSharpCodeBufferAsync();

        await _eventBus.PublishAsync(new OmniSharpAsyncBufferUpdateCompletedEvent(_environment.Script.Id));
    }

    private async Task UpdateOmniSharpCodeBufferWithDataConnectionProgramAsync(Task<DataConnectionSourceCode>? sourceCodeTask)
    {
        DataConnectionSourceCode? sourceCode = null;
        if (sourceCodeTask != null)
        {
            sourceCode = await sourceCodeTask;
        }

        string? dataConnectionProgramCode = null;

        if (sourceCode != null)
        {
            dataConnectionProgramCode = sourceCode.ApplicationCode.ToCodeString(true);
        }

        await UpdateBufferAsync(Project.DataConnectionProgramFilePath, dataConnectionProgramCode);
    }

    private async Task NotifyOmniSharpServerProjectFileChangedAsync()
    {
        await OmniSharpServer.SendAsync(new[]
        {
            new FilesChangedRequest
            {
                FileName = Project.ProjectFilePath,
                ChangeType = FileChangeType.Change
            }
        });
    }

    private async Task UpdateBufferAsync(string filePath, string? buffer)
    {
        var semaphore = _bufferUpdateSemaphores.GetOrAdd(filePath, key => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();

        try
        {
            buffer = !string.IsNullOrWhiteSpace(buffer) ? buffer : "//";

            await OmniSharpServer.SendAsync(new UpdateBufferRequest
            {
                FileName = filePath,
                Buffer = buffer
            });
        }
        finally
        {
            semaphore.Release();
        }
    }
}
