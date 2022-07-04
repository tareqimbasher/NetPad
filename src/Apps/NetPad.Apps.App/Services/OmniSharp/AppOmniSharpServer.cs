using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Utilities;
using OmniSharp;
using OmniSharp.Models.UpdateBuffer;

namespace NetPad.Services.OmniSharp;

/// <summary>
/// A wrapper around an <see cref="IOmniSharpServer"/> that includes app-specific functionality.
/// </summary>
public class AppOmniSharpServer
{
    private readonly ScriptEnvironment _environment;
    private readonly IOmniSharpServerFactory _omniSharpServerFactory;
    private readonly IOmniSharpServerLocator _omniSharpServerLocator;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly List<EventSubscriptionToken> _subscriptionTokens;

    private IOmniSharpServer? _omniSharpServer;
    private readonly ScriptProject _project;

    public AppOmniSharpServer(
        ScriptEnvironment environment,
        IOmniSharpServerFactory omniSharpServerFactory,
        IOmniSharpServerLocator omniSharpServerLocator,
        ICodeParser codeParser,
        IEventBus eventBus,
        ILogger<AppOmniSharpServer> logger,
        ILogger<ScriptProject> scriptProjectLogger)
    {
        _environment = environment;
        _omniSharpServerFactory = omniSharpServerFactory;
        _omniSharpServerLocator = omniSharpServerLocator;
        _eventBus = eventBus;
        _logger = logger;
        _subscriptionTokens = new();

        _project = new ScriptProject(environment.Script, codeParser, scriptProjectLogger);
    }

    public ScriptProject Project => _project;

    /// <summary>
    /// Sends a request to the server with no response.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if OmniSharp server has not been started yet.</exception>
    public Task Send(object request)
    {
        if (_omniSharpServer == null)
            throw new InvalidOperationException($"OmniSharp server has not been started yet. Script ID: {_environment.Script.Id}");

        return _omniSharpServer.Send(request);
    }

    /// <summary>
    /// Sends a request to the server and returns the server response.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if OmniSharp server has not been started yet.</exception>
    public Task<TResponse?> Send<TResponse>(object request) where TResponse : class
    {
        if (_omniSharpServer == null)
            throw new InvalidOperationException($"OmniSharp server has not been started yet. Script ID: {_environment.Script.Id}");

        return _omniSharpServer.Send<TResponse>(request);
    }

    /// <summary>
    /// Starts the server.
    /// </summary>
    public async Task<bool> StartAsync()
    {
        var omnisharpServerLocation = await _omniSharpServerLocator.GetServerLocationAsync();
        var executablePath = omnisharpServerLocation?.ExecutablePath;

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

        _logger.LogDebug("Initializing script project for script: {Script}", _environment.Script);
        await _project.InitializeAsync();

        var codeChangeToken = _eventBus.Subscribe<ScriptPropertyChanged>(async ev =>
        {
            if (ev.ScriptId != _environment.Script.Id || ev.PropertyName != nameof(Script.Code))
            {
                return;
            }

            if (_omniSharpServer == null)
            {
                return;
            }

            var fullProgram = await _project.UpdateProgramCodeAsync();

            if (_omniSharpServer != null)
            {
                await UpdateOmniSharpCodeBufferAsync(fullProgram);
            }
        });

        _subscriptionTokens.Add(codeChangeToken);

        var referencesChangeToken = _eventBus.Subscribe<ScriptReferencesUpdatedEvent>(async ev =>
        {
            foreach (var addedReference in ev.Added)
            {
                if (addedReference is PackageReference pkg)
                {
                    await _project.AddPackageAsync(pkg.PackageId, pkg.Version);
                }
            }

            foreach (var removedReference in ev.Removed)
            {
                if (removedReference is PackageReference pkg)
                {
                    await _project.RemovePackageAsync(pkg.PackageId);
                }
            }

            await StopOmniSharpServerAsync();
            await StartOmniSharpServerAsync(executablePath);

            // await Send(new UpdateBufferRequest
            // {
            //     FileName = _project.ProjectFilePath,
            //     Buffer = await File.ReadAllTextAsync(_project.ProjectFilePath)
            // });
            //
            // await Send(new FilesChangedRequest()
            // {
            //     FileName = _project.ProjectFilePath,
            //     Buffer = await File.ReadAllTextAsync(_project.ProjectFilePath),
            //     ChangeType = FileChangeType.Change
            // });
            //
            // await Send(new ChangeBufferRequest()
            // {
            //     FileName = _project.ProjectFilePath,
            //     NewText = await File.ReadAllTextAsync(_project.ProjectFilePath),
            //     StartLine = 1,
            //     StartColumn = 1,
            //     EndLine  = File.ReadAllLines(_project.ProjectFilePath).Length,
            //     EndColumn = File.ReadAllLines(_project.ProjectFilePath).Last().Length
            // });
            //
            // await Send(new ReAnalyzeRequest()
            // {
            //     FileName = _project.ProjectFilePath
            // });
        });

        _subscriptionTokens.Add(referencesChangeToken);

        await StartOmniSharpServerAsync(executablePath);

        return true;
    }

    /// <summary>
    /// Stops the server.
    /// </summary>
    public async Task StopAsync()
    {
        await StopOmniSharpServerAsync();

        foreach (var token in _subscriptionTokens)
        {
            _eventBus.Unsubscribe(token);
        }

        await _project.DeleteAsync();
    }

    private async Task StartOmniSharpServerAsync(string executablePath)
    {
        string args = new[]
        {
            $"--hostPID {Environment.ProcessId}",
            "--encoding utf-8",
            "--loglevel Information",
            "FileOptions:SystemExcludeSearchPatterns:0=**/.git",
            "FileOptions:SystemExcludeSearchPatterns:1=**/.svn",
            "FileOptions:SystemExcludeSearchPatterns:2=**/.hg",
            "FileOptions:SystemExcludeSearchPatterns:3=**/CVS",
            "FileOptions:SystemExcludeSearchPatterns:4=**/.DS_Store",
            "FileOptions:SystemExcludeSearchPatterns:5=**/Thumbs.db",
        }.JoinToString(" ");

        var omniSharpServer = _omniSharpServerFactory.CreateStdioServerFromNewProcess(executablePath, _project.ProjectDirectoryPath, args);

        _logger.LogDebug("Starting omnisharp server from path: {OmniSharpExePath} with args: {Args}", executablePath, args);

        await omniSharpServer.StartAsync();

        _omniSharpServer = omniSharpServer;
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

    private async Task UpdateOmniSharpCodeBufferAsync(string fullProgram)
    {
        await Send(new UpdateBufferRequest
        {
            FileName = _project.ProgramFilePath,
            Buffer = fullProgram
        });
    }
}
