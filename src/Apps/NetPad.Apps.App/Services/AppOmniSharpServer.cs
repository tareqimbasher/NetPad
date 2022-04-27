using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Utilities;
using OmniSharp;
using OmniSharp.Models.UpdateBuffer;

namespace NetPad.Services;

/// <summary>
/// A wrapper around an <see cref="IOmniSharpServer"/> used for app-specific functionality.
/// </summary>
public class AppOmniSharpServer
{
    private readonly ScriptEnvironment _environment;
    private readonly IOmniSharpServerFactory _omniSharpServerFactory;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly ICodeParser _codeParser;
    private readonly List<EventSubscriptionToken> _subscriptionTokens;

    private readonly string _projectDirectoryPath;
    private readonly string _programFilePath;
    private readonly string? _omnisharpExecutablePath;

    private IOmniSharpServer? _omniSharpServer;

    public AppOmniSharpServer(
        ScriptEnvironment environment,
        IOmniSharpServerFactory omniSharpServerFactory,
        ICodeParser codeParser,
        IEventBus eventBus,
        IConfiguration configuration,
        ILogger<AppOmniSharpServer> logger)
    {
        _environment = environment;
        _omniSharpServerFactory = omniSharpServerFactory;
        _eventBus = eventBus;
        _logger = logger;
        _codeParser = codeParser;
        _subscriptionTokens = new ();

        _projectDirectoryPath = Path.Combine(Path.GetTempPath(), "NetPad", _environment.Script.Id.ToString());
        _programFilePath = Path.Combine(_projectDirectoryPath, "Program.cs");
        _omnisharpExecutablePath = configuration.GetSection("OmniSharp").GetValue<string?>("ExecutablePath");
    }

    public int UserCodeStartsOnLine { get; private set; }
    public string ProgramFilePath => _programFilePath;

    public Task Send(object request)
    {
        if (_omniSharpServer == null)
            throw new InvalidOperationException($"OmniSharp server has not been started yet. Script ID: {_environment.Script.Id}");

        return _omniSharpServer.Send(request);
    }

    public Task<TResponse?> Send<TResponse>(object request) where TResponse : class
    {
        if (_omniSharpServer == null)
            throw new InvalidOperationException($"OmniSharp server has not been started yet. Script ID: {_environment.Script.Id}");

        return _omniSharpServer.Send<TResponse>(request);
    }

    public async Task<bool> StartAsync()
    {
        if (string.IsNullOrWhiteSpace(_omnisharpExecutablePath))
        {
            _logger.LogWarning($"OmniSharp executable path is not configured. OmniSharp functionality will not be enabled.");
            return false;
        }

        if (!File.Exists(_omnisharpExecutablePath))
        {
            _logger.LogWarning("OmniSharp executable path does not exist: {OmniSharpExecutablePath}. " +
                               "OmniSharp functionality will not be enabled", _omnisharpExecutablePath);
            return false;
        }

        Directory.CreateDirectory(_projectDirectoryPath);

        await UpdateProjectAsync();

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

            await UpdateProjectAsync();
        });

        _subscriptionTokens.Add(codeChangeToken);

        string args = new[]
        {
            //$"--hostPID {Environment.ProcessId}",
            "--encoding utf-8",
            //"--loglevel information",
            "FileOptions:SystemExcludeSearchPatterns:0=**/.git",
            "FileOptions:SystemExcludeSearchPatterns:1=**/.svn",
            "FileOptions:SystemExcludeSearchPatterns:2=**/.hg",
            "FileOptions:SystemExcludeSearchPatterns:3=**/CVS",
            "FileOptions:SystemExcludeSearchPatterns:4=**/.DS_Store",
            "FileOptions:SystemExcludeSearchPatterns:5=**/Thumbs.db",
        }.JoinToString(" ");

        var omniSharpServer = _omniSharpServerFactory.CreateStdioServerFromNewProcess(_omnisharpExecutablePath, _projectDirectoryPath, args);

        await omniSharpServer.StartAsync();

        _omniSharpServer = omniSharpServer;

        return true;
    }

    public async Task StopAsync()
    {
        if (_omniSharpServer != null)
        {
            await _omniSharpServer.StopAsync();
        }

        try
        {
            if (Directory.Exists(_projectDirectoryPath))
            {
                Directory.Delete(_projectDirectoryPath, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deleting temporary project directory at path: {ProjectDirectoryPath}", _projectDirectoryPath);
        }

        foreach (var token in _subscriptionTokens)
        {
            _eventBus.Unsubscribe(token);
        }
    }

    private async Task UpdateProjectAsync()
    {
        var projFilePath = Path.Combine(_projectDirectoryPath, "script.csproj");

        if (!File.Exists(projFilePath))
        {
            await File.WriteAllTextAsync(projFilePath, @"<Project Sdk=""Microsoft.NET.Sdk"">

    <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    </PropertyGroup>

</Project>
");
        }

        var parsingResult = _codeParser.Parse(_environment.Script);

        await File.WriteAllTextAsync(_programFilePath, parsingResult.FullProgram);

        UserCodeStartsOnLine = parsingResult.UserCodeStartLine;

        if (_omniSharpServer != null)
        {
            await Send(new UpdateBufferRequest
            {
                FileName = _programFilePath,
                Buffer = parsingResult.FullProgram
            });
        }
    }
}
