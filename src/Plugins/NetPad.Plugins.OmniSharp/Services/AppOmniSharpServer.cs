using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using OmniSharp.Models.Events;
using NetPad.Compilation;
using NetPad.Compilation.Scripts.Dependencies;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.Data.Events;
using NetPad.Data.Metadata;
using NetPad.DotNet;
using NetPad.DotNet.References;
using NetPad.Events;
using NetPad.Plugins.OmniSharp.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;
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
public class AppOmniSharpServer(
    ScriptEnvironment environment,
    IOmniSharpServerFactory omniSharpServerFactory,
    IOmniSharpServerLocator omniSharpServerLocator,
    IDataConnectionResourcesCache dataConnectionResourcesCache,
    IScriptDependencyResolver scriptDependencyResolver,
    Settings settings,
    ICodeParser codeParser,
    IEventBus eventBus,
    IDotNetInfo dotNetInfo,
    ILogger<AppOmniSharpServer> logger,
    ILogger<OmniSharpProject> scriptProjectLogger)
{
    private const int MaxAutoRestartAttempts = 3;
    private static readonly TimeSpan _autoRestartStabilityThreshold = TimeSpan.FromSeconds(60);

    private readonly ILogger _logger = logger;
    private readonly List<EventSubscriptionToken> _subscriptionTokens = [];
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _bufferUpdateSemaphores = new();
    private readonly CodeParsingOptions _codeParsingOptions = new();
    private IOmniSharpStdioServer? _omniSharpServer;
    private CancellationTokenSource? _backgroundTasksCts;
    private int _autoRestartCount;
    private DateTime _lastSuccessfulStart;
    private int _isRestarting;

    private CancellationToken BackgroundCancellationToken => _backgroundTasksCts?.Token ?? CancellationToken.None;

    public Guid ScriptId => environment.Script.Id;

    public OmniSharpProject Project { get; } = new(environment.Script, dotNetInfo, settings, scriptProjectLogger);

    public IOmniSharpStdioServer OmniSharpServer => _omniSharpServer ?? throw new InvalidOperationException(
        $"OmniSharp server has not been started yet. Script ID: {environment.Script.Id}");

    /// <summary>
    /// Starts the server.
    /// </summary>
    public async Task<bool> StartAsync()
    {
        _logger.LogDebug("Initializing script project for script: {Script}", environment.Script);

        Project.ProjectDirectoryPath.DeleteIfExists();

        await Project.CreateAsync(
            environment.Script.Config.TargetFrameworkVersion,
            ProjectOutputType.Executable,
            environment.Script.Config.UseAspNet ? DotNetSdkPack.AspNetApp : DotNetSdkPack.NetApp,
            true,
            false);

        await Project.SetProjectGroupItemAsync("AllowUnsafeBlocks", "true");

        await SetPreprocessorSymbolsAsync();

        await Project.AddReferencesAsync(
            scriptDependencyResolver
                .GetUserVisibleAssemblies()
                .Select(a => new AssemblyFileReference(a)));

        await Project.RestoreAsync();

        // Write initial code to disk so OmniSharp picks it up during project load,
        // rather than needing a post-load buffer update round-trip.
        if (!string.IsNullOrWhiteSpace(environment.Script.Code))
        {
            WriteInitialCodeToDisk();
        }

        InitializeEventHandlers();

        await StartOmniSharpServerAsync();

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

            var symbols = string.Join(";", PreprocessorSymbols.For(environment.Script.Config.OptimizationLevel)) + ";";

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
            eventBus.Unsubscribe(token);
        }

        await StopOmniSharpServerAsync();

        var semaphores = _bufferUpdateSemaphores.Values.ToArray();
        foreach (var semaphore in semaphores)
        {
            semaphore.Dispose();
        }

        _bufferUpdateSemaphores.Clear();

        Project.Delete();
    }

    public async Task<bool> RestartAsync(Action<string>? progress = null)
    {
        _autoRestartCount = 0;

        progress?.Invoke("Stopping OmniSharp server...");
        await StopOmniSharpServerAsync();

        progress?.Invoke("Starting OmniSharp server...");
        await StartOmniSharpServerAsync();

        await eventBus.PublishAsync(new OmniSharpServerRestartedEvent(this));

        return true;
    }

    private async Task StartOmniSharpServerAsync()
    {
        var omnisharpServerLocation = await omniSharpServerLocator.GetServerLocationAsync();
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

            "Sdk:IncludePrereleases=true",

            "FileOptions:SystemExcludeSearchPatterns:0=**/.git",
            "FileOptions:SystemExcludeSearchPatterns:1=**/.svn",
            "FileOptions:SystemExcludeSearchPatterns:2=**/.hg",
            "FileOptions:SystemExcludeSearchPatterns:3=**/CVS",
            "FileOptions:SystemExcludeSearchPatterns:4=**/.DS_Store",
            "FileOptions:SystemExcludeSearchPatterns:5=**/Thumbs.db",
            $"RoslynExtensionsOptions:EnableAnalyzersSupport={settings.OmniSharp.EnableAnalyzersSupport}",
            "RoslynExtensionsOptions:EnableEditorConfigSupport=false",
            "RoslynExtensionsOptions:EnableDecompilationSupport=true",
            $"RoslynExtensionsOptions:EnableImportCompletion={settings.OmniSharp.EnableImportCompletion}",
            "RoslynExtensionsOptions:EnableAsyncCompletion=false",

            $"RoslynExtensionsOptions:InlayHintsOptions:EnableForParameters={settings.OmniSharp.InlayHints.EnableParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForLiteralParameters={settings.OmniSharp.InlayHints.EnableLiteralParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForIndexerParameters={settings.OmniSharp.InlayHints.EnableIndexerParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForObjectCreationParameters={settings.OmniSharp.InlayHints.EnableObjectCreationParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForOtherParameters={settings.OmniSharp.InlayHints.EnableOtherParameters}",
            $"RoslynExtensionsOptions:InlayHintsOptions:SuppressForParametersThatDifferOnlyBySuffix={settings.OmniSharp.InlayHints.SuppressForParametersThatDifferOnlyBySuffix}",
            $"RoslynExtensionsOptions:InlayHintsOptions:SuppressForParametersThatMatchMethodIntent={settings.OmniSharp.InlayHints.SuppressForParametersThatMatchMethodIntent}",
            $"RoslynExtensionsOptions:InlayHintsOptions:SuppressForParametersThatMatchArgumentName={settings.OmniSharp.InlayHints.SuppressForParametersThatMatchArgumentName}",
            $"RoslynExtensionsOptions:InlayHintsOptions:EnableForTypes={settings.OmniSharp.InlayHints.EnableTypes}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForImplicitVariableTypes={settings.OmniSharp.InlayHints.EnableImplicitVariableTypes}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForLambdaParameterTypes={settings.OmniSharp.InlayHints.EnableLambdaParameterTypes}",
            $"RoslynExtensionsOptions:InlayHintsOptions:ForImplicitObjectCreation={settings.OmniSharp.InlayHints.EnableImplicitObjectCreation}"
        }.JoinToString(" ");

        var dotNetRootDir = dotNetInfo.LocateDotNetRootDirectoryForFramework(
            environment.Script.Config.TargetFrameworkVersion) ?? dotNetInfo.LocateDotNetRootDirectory();

        var omniSharpServer = omniSharpServerFactory.CreateStdioServerFromNewProcess(
            executablePath!,
            Project.ProjectDirectoryPath.Path,
            args,
            dotNetRootDir);

        _logger.LogDebug(
            "Starting omnisharp server\nFrom path: {OmniSharpExePath}\nProject dir: {ProjDirPath}\nWith args: {Args}",
            executablePath,
            Project.ProjectDirectoryPath,
            args);

        await omniSharpServer.StartAsync();

        omniSharpServer.OnProcessUnexpectedExit = HandleProcessUnexpectedExit;
        _omniSharpServer = omniSharpServer;
        _backgroundTasksCts = new CancellationTokenSource();
        _lastSuccessfulStart = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(environment.Script.Code))
        {
            var ct = _backgroundTasksCts.Token;
            var bufferUpdated = 0;
            var projectEventTokens = new List<IDisposable>(2);

            async Task UpdateBuffersOnProjectEvent(JsonNode _)
            {
                if (Interlocked.CompareExchange(ref bufferUpdated, 1, 0) != 0)
                    return;

                try
                {
                    bool shouldUpdateDataConnectionCodeBuffer =
                        environment.Script.DataConnection == null
                        || await dataConnectionResourcesCache.HasCachedResourcesAsync(
                            environment.Script.DataConnection.Id,
                            environment.Script.Config.TargetFrameworkVersion
                        );

                    if (shouldUpdateDataConnectionCodeBuffer)
                        await UpdateOmniSharpCodeBufferWithDataConnectionAsync(environment.Script.DataConnection);
                    else
                        await UpdateOmniSharpCodeBufferAsync();

                    foreach (var token in projectEventTokens)
                    {
                        token.Dispose();
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to update OmniSharp buffers after project loaded");
                    Interlocked.Exchange(ref bufferUpdated, 0);
                }
            }

            projectEventTokens.Add(omniSharpServer.SubscribeToEvent(EventTypes.ProjectAdded,
                UpdateBuffersOnProjectEvent));
            projectEventTokens.Add(omniSharpServer.SubscribeToEvent(EventTypes.ProjectChanged,
                UpdateBuffersOnProjectEvent));

            // Fallback if no event fires
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(20), ct);
                    if (Interlocked.CompareExchange(ref bufferUpdated, 0, 0) == 0)
                    {
                        _logger.LogWarning(
                            "No ProjectAdded/ProjectChanged event received within 20s, updating buffers as fallback");
                        await UpdateBuffersOnProjectEvent(null!);
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }, ct);
        }

        await eventBus.PublishAsync(new OmniSharpServerStartedEvent(this));
    }

    private async Task StopOmniSharpServerAsync()
    {
        if (_backgroundTasksCts != null)
        {
            await _backgroundTasksCts.CancelAsync();
            _backgroundTasksCts.Dispose();
            _backgroundTasksCts = null;
        }

        if (_omniSharpServer == null)
        {
            return;
        }

        _omniSharpServer.OnProcessUnexpectedExit = null;
        await _omniSharpServer.StopAsync();

        _omniSharpServer = null;

        await eventBus.PublishAsync(new OmniSharpServerStoppedEvent(this));
    }

    private void HandleProcessUnexpectedExit()
    {
        if (Interlocked.CompareExchange(ref _isRestarting, 1, 0) != 0)
        {
            return;
        }

        _logger.LogWarning(
            "OmniSharp server process exited unexpectedly for script {ScriptId}. Attempting auto-restart...",
            environment.Script.Id);

        // Reset counter if the server was stable long enough
        if (DateTime.UtcNow - _lastSuccessfulStart > _autoRestartStabilityThreshold)
        {
            _autoRestartCount = 0;
        }

        _autoRestartCount++;

        if (_autoRestartCount > MaxAutoRestartAttempts)
        {
            _logger.LogError(
                "OmniSharp server for script {ScriptId} has crashed {Count} times. Not restarting. Use manual restart.",
                environment.Script.Id, _autoRestartCount);

            Interlocked.Exchange(ref _isRestarting, 0);
            _ = eventBus.PublishAsync(new OmniSharpServerStoppedEvent(this));
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                // Cancel old background tasks first so they can't fire against a null server
                if (_backgroundTasksCts != null)
                {
                    await _backgroundTasksCts.CancelAsync();
                    _backgroundTasksCts.Dispose();
                    _backgroundTasksCts = null;
                }

                // Unsubscribe from the dead server before dropping the reference
                var deadServer = _omniSharpServer;
                if (deadServer != null)
                {
                    deadServer.OnProcessUnexpectedExit = null;
                }

                _omniSharpServer = null;

                await StartOmniSharpServerAsync();

                _logger.LogInformation(
                    "OmniSharp server auto-restarted successfully for script {ScriptId} (attempt {Count}/{Max})",
                    environment.Script.Id, _autoRestartCount, MaxAutoRestartAttempts);

                await eventBus.PublishAsync(new OmniSharpServerRestartedEvent(this));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to auto-restart OmniSharp server for script {ScriptId} (attempt {Count}/{Max})",
                    environment.Script.Id, _autoRestartCount, MaxAutoRestartAttempts);

                await eventBus.PublishAsync(new OmniSharpServerStoppedEvent(this));
            }
            finally
            {
                Interlocked.Exchange(ref _isRestarting, 0);
            }
        });
    }

    private bool IsValidServerExecutablePath(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            _logger.LogError(
                "Could not locate the OmniSharp Server executable. OmniSharp functionality will be disabled");
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
            if (ev.Script.Id != environment.Script.Id) return;
            await UpdateOmniSharpCodeBufferAsync();
        });

        Subscribe<ScriptTargetFrameworkVersionUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != environment.Script.Id) return;

            await Project.UpdateTargetFrameworkAsync(ev.NewVersion);
            await Project.RestoreAsync();

            // OmniSharp must be restarted because DOTNET_ROOT (set at process startup) may
            // point to a different .NET installation for the new target framework.
            await RestartAsync();

            if (ev.Script.DataConnection != null)
            {
                var ct = BackgroundCancellationToken;

                // When target framework version changes, we need to update OmniSharp's data connection assembly references
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UpdateOmniSharpCodeBufferWithDataConnectionAsync(ev.Script.DataConnection);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex,
                            "Failed to update OmniSharp data connection buffers after target framework change");
                    }
                }, ct);
            }
        });

        Subscribe<ScriptOptimizationLevelUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != environment.Script.Id) return;

            await SetPreprocessorSymbolsAsync();
            await NotifyOmniSharpServerProjectFileChangedAsync();
            await eventBus.PublishAsync(new OmniSharpAsyncBufferUpdateCompletedEvent(environment.Script.Id));
        });

        Subscribe<ScriptUseAspNetUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != environment.Script.Id) return;

            await Project.SetProjectAttributeAsync(
                "Sdk",
                DotNetCSharpProject.GetProjectSdkName(ev.NewValue ? DotNetSdkPack.AspNetApp : DotNetSdkPack.NetApp));
            await NotifyOmniSharpServerProjectFileChangedAsync();
            await eventBus.PublishAsync(new OmniSharpAsyncBufferUpdateCompletedEvent(environment.Script.Id));
        });

        Subscribe<ScriptNamespacesUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != environment.Script.Id) return;
            await UpdateOmniSharpCodeBufferAsync();
        });

        Subscribe<ScriptReferencesUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != environment.Script.Id) return;

            if (!ev.Added.Any() && !ev.Removed.Any()) return;

            await Project.AddReferencesAsync(ev.Added);

            await Project.RemoveReferencesAsync(ev.Removed);

            await NotifyOmniSharpServerProjectFileChangedAsync();
            await eventBus.PublishAsync(new OmniSharpAsyncBufferUpdateCompletedEvent(environment.Script.Id));
        });

        Subscribe<DataConnectionResourcesUpdatedEvent>(ev =>
        {
            if (environment.Script.DataConnection == null
                || ev.DataConnection.Id != environment.Script.DataConnection.Id
                || ev.TargetFrameworkVersion != environment.Script.Config.TargetFrameworkVersion)
            {
                return Task.CompletedTask;
            }

            var dataConnection = ev.DataConnection;
            var ct = BackgroundCancellationToken;

            Task.Run(async () =>
            {
                try
                {
                    await UpdateOmniSharpCodeBufferWithDataConnectionAsync(dataConnection);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to update OmniSharp data connection buffers after resource update");
                }
            }, ct);

            return Task.CompletedTask;
        });

        Subscribe<ScriptDataConnectionChangedEvent>(ev =>
        {
            if (ev.Script.Id != environment.Script.Id) return Task.CompletedTask;

            var dataConnection = ev.DataConnection;
            var ct = BackgroundCancellationToken;

            Task.Run(async () =>
            {
                try
                {
                    await UpdateOmniSharpCodeBufferWithDataConnectionAsync(dataConnection);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to update OmniSharp data connection buffers after connection change");
                }
            }, ct);

            return Task.CompletedTask;
        });
    }

    private void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class, IEvent
    {
        var token = eventBus.Subscribe<TEvent>(ev =>
        {
            if (_omniSharpServer != null && _backgroundTasksCts is { IsCancellationRequested: false })
            {
                _ = handler(ev).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        _logger.LogError(t.Exception.InnerException ?? t.Exception,
                            "Error in OmniSharp event handler for {EventType}",
                            typeof(TEvent).Name);
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            return Task.CompletedTask;
        });
        _subscriptionTokens.Add(token);
    }

    #endregion

    /// <summary>
    /// Writes parsed script code to the project's .cs files on disk before OmniSharp starts,
    /// so OmniSharp picks up the correct code during its initial project load.
    /// </summary>
    private void WriteInitialCodeToDisk()
    {
        _codeParsingOptions.IncludeAspNetUsings = environment.Script.Config.UseAspNet;
        var parsingResult = codeParser.Parse(environment.Script, options: _codeParsingOptions);

        var userCode = parsingResult.UserProgram.Code.Value;
        File.WriteAllText(Project.UserProgramFilePath,
            !string.IsNullOrWhiteSpace(userCode) ? userCode : "//");

        var usings = parsingResult.GetFullProgram().GetAllUsings()
            .Select(u => u.ToCodeString(true))
            .JoinToString(Environment.NewLine);
        var bootstrapperCode = $"{usings}\n\n{parsingResult.BootstrapperProgram.Code.ToCodeString()}";
        File.WriteAllText(Project.BootstrapperProgramFilePath, bootstrapperCode);
    }

    private async Task UpdateOmniSharpCodeBufferAsync(bool publishCompletedEvent = true)
    {
        var script = environment.Script;
        _codeParsingOptions.IncludeAspNetUsings = script.Config.UseAspNet;

        var parsingResult = codeParser.Parse(script, options: _codeParsingOptions);
        await UpdateOmniSharpCodeBufferWithBootstrapperProgramAsync(parsingResult);
        await UpdateOmniSharpCodeBufferWithUserProgramAsync(parsingResult);

        if (publishCompletedEvent)
        {
            await eventBus.PublishAsync(new OmniSharpAsyncBufferUpdateCompletedEvent(environment.Script.Id));
        }
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
        var connectionResources = dataConnection == null
            ? null
            : await dataConnectionResourcesCache.GetResourcesAsync(
                dataConnection,
                environment.Script.Config.TargetFrameworkVersion);

        List<Reference> references = [];

        if (connectionResources?.RequiredReferences?.Length > 0)
        {
            references.AddRange(connectionResources.RequiredReferences);
        }

        if (connectionResources?.Assembly != null)
        {
            references.Add(new AssemblyImageReference(connectionResources.Assembly));
        }

        await Project.UpdateReferencesFromDataConnectionAsync(dataConnection, references);
        await NotifyOmniSharpServerProjectFileChangedAsync();
        await UpdateOmniSharpCodeBufferWithDataConnectionProgramAsync(connectionResources?.SourceCode);
        await UpdateOmniSharpCodeBufferAsync(publishCompletedEvent: false);

        // Notify project file changed again
        await NotifyOmniSharpServerProjectFileChangedAsync();

        await eventBus.PublishAsync(new OmniSharpAsyncBufferUpdateCompletedEvent(environment.Script.Id));
    }

    private async Task UpdateOmniSharpCodeBufferWithDataConnectionProgramAsync(DataConnectionSourceCode? sourceCode)
    {
        string? dataConnectionProgramCode = null;

        if (sourceCode != null)
        {
            dataConnectionProgramCode = sourceCode.ApplicationCode.ToCodeString(true);
        }

        await UpdateBufferAsync(Project.DataConnectionProgramFilePath, dataConnectionProgramCode);
    }

    private async Task NotifyOmniSharpServerProjectFileChangedAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await OmniSharpServer.SendAsync(new[]
            {
                new FilesChangedRequest
                {
                    FileName = Project.ProjectFilePath.Path,
                    ChangeType = FileChangeType.Create
                }
            }, cts.Token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to notify OmniSharp server that project file changed");
            throw;
        }
    }

    private async Task UpdateBufferAsync(string filePath, string? buffer)
    {
        var semaphore = _bufferUpdateSemaphores.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();

        try
        {
            buffer = !string.IsNullOrWhiteSpace(buffer) ? buffer : "//";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await OmniSharpServer.SendAsync(new UpdateBufferRequest
            {
                FileName = filePath,
                Buffer = buffer
            }, cts.Token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update OmniSharp server buffer for file: '{FilePath}'", filePath);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }
}
