using System.Collections.Concurrent;
using System.Xml.Linq;
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
/// A wrapper around an <see cref="IOmniSharpServer"/> that includes app-specific functionality. Each script
/// has its own instance of an <see cref="AppOmniSharpServer"/>.
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
    private readonly IDotNetInfo _dotNetInfo;
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
        _dotNetInfo = dotNetInfo;
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
        _logger.LogDebug("Initializing script project for script: {Script}", _environment.Script);

        await Project.CreateAsync(
            _environment.Script.Config.TargetFrameworkVersion,
            ProjectOutputType.Executable,
            _environment.Script.Config.UseAspNet ? DotNetSdkPack.AspNetApp : DotNetSdkPack.NetApp,
            true);

        await Project.SetProjectPropertyAsync("AllowUnsafeBlocks", "true");

        await SetPreprocessorSymbolsAsync();

        await Project.AddReferencesAsync(_environment.GetScriptRuntimeUserAccessibleAssemblies().Select(a => new AssemblyFileReference(a)));

        await Project.RestoreAsync();

        InitializeEventHandlers();

        await StartOmniSharpServerAsync();

        await _eventBus.PublishAsync(new OmniSharpServerStartedEvent(this));

        return true;
    }

    private async Task SetPreprocessorSymbolsAsync()
    {
        await Project.ModifyProjectFileAsync(root =>
        {
            var existing = root.Elements()
                .FirstOrDefault(el =>
                {
                    var children = el.Elements().ToArray();

                    return children.Length == 1 && children[0].Name == "DefineConstants";
                });

            // Remove the existing group
            existing?.Remove();

            var symbols = string.Join(";", PreprocessorSymbols.For(_environment.Script.Config.OptimizationLevel)) + ";";

            // Add a new group
            root.Add(XElement.Parse(@$"<PropertyGroup>
    <DefineConstants>{symbols}</DefineConstants>
</PropertyGroup>"));
        });
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

        _bufferUpdateSemaphores.Clear();

        await _eventBus.PublishAsync(new OmniSharpServerStoppedEvent(this));

        await Project.DeleteAsync();
    }

    public async Task<bool> RestartAsync(Action<string>? progress = null)
    {
        progress?.Invoke("Stopping OmniSharp server...");
        await StopOmniSharpServerAsync();

        progress?.Invoke("Starting OmniSharp server...");
        await StartOmniSharpServerAsync();

        await _eventBus.PublishAsync(new OmniSharpServerRestartedEvent(this));

        return true;
    }

    private async Task StartOmniSharpServerAsync()
    {
        var omnisharpServerLocation = await _omniSharpServerLocator.GetServerLocationAsync();
        var executablePath = omnisharpServerLocation?.ExecutablePath;

        if (!IsValidServerExecutablePath(executablePath))
        {
            throw new Exception($"Server executable path: {executablePath} is not valid");
        }

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

        var omniSharpServer = _omniSharpServerFactory.CreateStdioServerFromNewProcess(
            executablePath!,
            Project.ProjectDirectoryPath,
            args,
            _dotNetInfo.LocateDotNetRootDirectory());

        _logger.LogDebug("Starting omnisharp server\nFrom path: {OmniSharpExePath}\nProject dir: {ProjDirPath}\nWith args: {Args}",
            executablePath,
            Project.ProjectDirectoryPath,
            args);

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
                                                            || await _dataConnectionResourcesCache.HasCachedResourcesAsync(
                                                                _environment.Script.DataConnection.Id,
                                                                _environment.Script.Config.TargetFrameworkVersion);
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

        Subscribe<ScriptTargetFrameworkVersionUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != _environment.Script.Id) return;

            await Project.SetProjectPropertyAsync("TargetFramework", ev.NewVersion.GetTargetFrameworkMoniker());

            if (ev.Script.DataConnection == null)
            {
                await NotifyOmniSharpServerProjectFileChangedAsync();
            }
            else
            {
                // When target framework version changes, we need to update OmniSharp's data connection assembly references
                _ = Task.Run(async () => { await UpdateOmniSharpCodeBufferWithDataConnectionAsync(ev.Script.DataConnection); });
            }
        });

        Subscribe<ScriptOptimizationLevelUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != _environment.Script.Id) return;

            await SetPreprocessorSymbolsAsync();
            await NotifyOmniSharpServerProjectFileChangedAsync();
        });

        Subscribe<ScriptUseAspNetUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != _environment.Script.Id) return;

            await Project.SetProjectAttributeAsync("Sdk", DotNetCSharpProject.GetProjectSdkName(ev.NewValue ? DotNetSdkPack.AspNetApp : DotNetSdkPack.NetApp));
            await NotifyOmniSharpServerProjectFileChangedAsync();
        });

        Subscribe<ScriptNamespacesUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != _environment.Script.Id) return;
            await UpdateOmniSharpCodeBufferAsync();
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

    private async Task UpdateOmniSharpCodeBufferAsync()
    {
        var script = _environment.Script;
        var parsingResult = _codeParser.Parse(script.Code, script.Config.Kind, script.Config.Namespaces, new CodeParsingOptions()
        {
            IncludeAspNetUsings = script.Config.UseAspNet,
        });
        await UpdateOmniSharpCodeBufferWithBootstrapperProgramAsync(parsingResult);
        await UpdateOmniSharpCodeBufferWithUserProgramAsync(parsingResult);
    }

    private async Task UpdateOmniSharpCodeBufferWithUserProgramAsync(CodeParsingResult parsingResult)
    {
        await UpdateBufferAsync(Project.UserProgramFilePath, parsingResult.UserProgram.Code.Value);
    }

    private async Task UpdateOmniSharpCodeBufferWithBootstrapperProgramAsync(CodeParsingResult parsingResult)
    {
        var usings = parsingResult.GetFullProgram().GetAllUsings()
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
            references.AddRange(
                await _dataConnectionResourcesCache.GetRequiredReferencesAsync(dataConnection, _environment.Script.Config.TargetFrameworkVersion));

            var assembly = await _dataConnectionResourcesCache.GetAssemblyAsync(dataConnection, _environment.Script.Config.TargetFrameworkVersion);
            if (assembly != null)
                references.Add(new AssemblyImageReference(assembly));
        }

        await Project.UpdateReferencesFromDataConnectionAsync(dataConnection, references);
        await NotifyOmniSharpServerProjectFileChangedAsync();

        var sourceCode = dataConnection == null
            ? null
            : _dataConnectionResourcesCache.GetSourceGeneratedCodeAsync(dataConnection, _environment.Script.Config.TargetFrameworkVersion);
        await UpdateOmniSharpCodeBufferWithDataConnectionProgramAsync(sourceCode);

        // Needed to trigger diagnostics and semantic highlighting for script file
        await Task.Delay(1000);
        await UpdateOmniSharpCodeBufferAsync();
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

        await _eventBus.PublishAsync(new OmniSharpAsyncBufferUpdateCompletedEvent(_environment.Script.Id));
    }

    private async Task UpdateBufferAsync(string filePath, string? buffer)
    {
        var semaphore = _bufferUpdateSemaphores.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
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

        await _eventBus.PublishAsync(new OmniSharpAsyncBufferUpdateCompletedEvent(_environment.Script.Id));
    }
}
