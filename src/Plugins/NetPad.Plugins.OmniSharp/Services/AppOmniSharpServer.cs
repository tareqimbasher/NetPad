using System.Collections.Concurrent;
using System.Xml.Linq;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.Data.Events;
using NetPad.DotNet;
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
    OmniSharpServerBuilder omniSharpServerBuilder,
    IDataConnectionResourcesCache dataConnectionResourcesCache,
    Settings settings,
    ICodeParser codeParser,
    IEventBus eventBus,
    IDotNetInfo dotNetInfo,
    ILogger<AppOmniSharpServer> logger,
    ILogger<OmniSharpProject> scriptProjectLogger)
{
    private readonly ILogger _logger = logger;
    private readonly List<EventSubscriptionToken> _subscriptionTokens = [];
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _bufferUpdateSemaphores = new();

    private IOmniSharpStdioServer? _omniSharpServer;

    public Guid ScriptId => environment.Script.Id;

    public OmniSharpProject Project { get; } = new(environment.Script, dotNetInfo, settings, scriptProjectLogger);

    public IOmniSharpStdioServer OmniSharpServer => _omniSharpServer
                                                    ?? throw new InvalidOperationException(
                                                        $"OmniSharp server has not been started yet. Script ID: {environment.Script.Id}");

    /// <summary>
    /// Starts the server.
    /// </summary>
    public async Task<bool> StartAsync()
    {
        _logger.LogDebug("Initializing script project for script: {Script}", environment.Script);

        await Project.CreateAsync(
            environment.Script.Config.TargetFrameworkVersion,
            ProjectOutputType.Executable,
            environment.Script.Config.UseAspNet ? DotNetSdkPack.AspNetApp : DotNetSdkPack.NetApp,
            true,
            true,
            false);

        await Project.SetProjectPropertyAsync("AllowUnsafeBlocks", "true");

        await SetPreprocessorSymbolsAsync();

        await Project.AddReferencesAsync(
            environment.GetUserVisibleAssemblies().Select(a => new AssemblyFileReference(a)));

        await Project.RestoreAsync();

        InitializeEventHandlers();

        await StartOmniSharpServerAsync();

        await eventBus.PublishAsync(new OmniSharpServerStartedEvent(this));

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

        await eventBus.PublishAsync(new OmniSharpServerStoppedEvent(this));

        await Project.DeleteAsync();
    }

    public async Task<bool> RestartAsync(Action<string>? progress = null)
    {
        progress?.Invoke("Stopping OmniSharp server...");
        await StopOmniSharpServerAsync();

        progress?.Invoke("Starting OmniSharp server...");
        await StartOmniSharpServerAsync();

        await eventBus.PublishAsync(new OmniSharpServerRestartedEvent(this));

        return true;
    }

    private async Task StartOmniSharpServerAsync()
    {
        var omniSharpServer = await omniSharpServerBuilder.BuildAsync(Project);

        await omniSharpServer.StartAsync();

        _omniSharpServer = omniSharpServer;

        // It takes some time for OmniSharp to register its updated buffer after it starts
        if (!string.IsNullOrWhiteSpace(environment.Script.Code))
        {
            // We don't want to await
#pragma warning disable CS4014
            Task.Run(async () =>
#pragma warning restore CS4014
            {
                await Task.Delay(3000);

                bool shouldUpdateDataConnectionCodeBuffer = environment.Script.DataConnection == null
                                                            || await dataConnectionResourcesCache.HasCachedResourcesAsync(
                                                                environment.Script.DataConnection.Id,
                                                                environment.Script.Config.TargetFrameworkVersion);
                if (shouldUpdateDataConnectionCodeBuffer)
                    await UpdateOmniSharpCodeBufferWithDataConnectionAsync(environment.Script.DataConnection);
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

    # region Event Handlers

    private void InitializeEventHandlers()
    {
        Subscribe<ScriptCodeUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != environment.Script.Id) return;

            await UpdateOmniSharpCodeBufferAsync();

            // Technically we should be doing this, instead of the previous line however when
            // we do, code in bootstrapper program like the .Dump() extension method is not recognized
            // TODO need to find a point where we can determine OmniSharp has fully started and is ready to update buffer and for it to register
            // var parsingResult = _codeParser.Parse(_environment.Script);
            // await UpdateOmniSharpCodeBufferWithUserProgramAsync(parsingResult);
        });

        Subscribe<ScriptTargetFrameworkVersionUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != environment.Script.Id) return;

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
            if (ev.Script.Id != environment.Script.Id) return;

            await SetPreprocessorSymbolsAsync();
            await NotifyOmniSharpServerProjectFileChangedAsync();
        });

        Subscribe<ScriptUseAspNetUpdatedEvent>(async ev =>
        {
            if (ev.Script.Id != environment.Script.Id) return;

            await Project.SetProjectAttributeAsync("Sdk", DotNetCSharpProject.GetProjectSdkName(ev.NewValue ? DotNetSdkPack.AspNetApp : DotNetSdkPack.NetApp));
            await NotifyOmniSharpServerProjectFileChangedAsync();
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

            Task.Run(async () => { await UpdateOmniSharpCodeBufferWithDataConnectionAsync(dataConnection); });

            return Task.CompletedTask;
        });

        Subscribe<ScriptDataConnectionChangedEvent>(ev =>
        {
            if (ev.Script.Id != environment.Script.Id) return Task.CompletedTask;

            var dataConnection = ev.DataConnection;

            Task.Run(async () => { await UpdateOmniSharpCodeBufferWithDataConnectionAsync(dataConnection); });

            return Task.CompletedTask;
        });
    }

    private void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class, IEvent
    {
        var token = eventBus.Subscribe<TEvent>(ev =>
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
        var script = environment.Script;
        var parsingResult = codeParser.Parse(script.Code, script.Config.Kind, script.Config.Namespaces, new CodeParsingOptions()
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
        var connectionResources = dataConnection == null
            ? null
            : await dataConnectionResourcesCache.GetResourcesAsync(dataConnection, environment.Script.Config.TargetFrameworkVersion);

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

        // Needed to trigger diagnostics and semantic highlighting for script file
        await Task.Delay(1000);
        await UpdateOmniSharpCodeBufferAsync();
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
        await OmniSharpServer.SendAsync(new[]
        {
            new FilesChangedRequest
            {
                FileName = Project.ProjectFilePath,
                ChangeType = FileChangeType.Change
            }
        });

        await eventBus.PublishAsync(new OmniSharpAsyncBufferUpdateCompletedEvent(environment.Script.Id));
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

        await eventBus.PublishAsync(new OmniSharpAsyncBufferUpdateCompletedEvent(environment.Script.Id));
    }
}
