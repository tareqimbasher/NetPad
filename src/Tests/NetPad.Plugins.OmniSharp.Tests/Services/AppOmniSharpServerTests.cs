using System.Globalization;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetPad.Apps.ProcessSetup;
using NetPad.Common;
using NetPad.Compilation;
using NetPad.Compilation.Scripts.Dependencies;
using NetPad.Configuration;
using NetPad.Data.Metadata;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.ExecutionModel;
using NetPad.Plugins.OmniSharp.Events;
using NetPad.Plugins.OmniSharp.Services;
using NetPad.Scripts;
using OmniSharp;
using OmniSharp.Models.Events;
using OmniSharp.Stdio;

namespace NetPad.Plugins.OmniSharp.Tests.Services;

[CollectionDefinition(Name, DisableParallelization = true)]
public class ProcessLanguageCollection
{
    public const string Name = "Changes the UI language of the test process";
}

[Collection(ProcessLanguageCollection.Name)]
public sealed class AppOmniSharpServerTests : IDisposable
{
    private static readonly TimeSpan _eventTimeout = TimeSpan.FromSeconds(10);

    /// <summary>How long to wait before concluding that an event will not be published.</summary>
    private static readonly TimeSpan _noEventGracePeriod = TimeSpan.FromMilliseconds(500);

    private readonly string _executablePath = Path.GetTempFileName();
    private readonly string _runtimeConfigPath;
    private readonly string _dotNetRoot = Directory.CreateTempSubdirectory().FullName;
    private readonly Mock<IDotNetInfo> _dotNetInfo = new();
    private readonly ServiceProvider _serviceProvider;
    private readonly ScriptEnvironment _environment;
    private readonly Mock<IOmniSharpServerFactory> _serverFactory = new();
    private readonly Mock<IOmniSharpServerLocator> _locator = new();
    private readonly Settings _settings;
    private readonly List<FakeOmniSharpProcess> _processes = [];
    private readonly Channel<OmniSharpServerReadyEvent> _readyEvents = Channel.CreateUnbounded<OmniSharpServerReadyEvent>();
    private readonly AppOmniSharpServer _server;

    public AppOmniSharpServerTests()
    {
        _runtimeConfigPath = Path.ChangeExtension(_executablePath, ".runtimeconfig.json");

        var runnerFactory = new Mock<IScriptRunnerFactory>();
        runnerFactory.Setup(f => f.CreateRunner(It.IsAny<Script>())).Returns(Mock.Of<IScriptRunner>());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton(runnerFactory.Object);
        _serviceProvider = services.BuildServiceProvider();

        var eventBus = _serviceProvider.GetRequiredService<IEventBus>();
        eventBus.Subscribe<OmniSharpServerReadyEvent>(ev => _readyEvents.Writer.WriteAsync(ev).AsTask());

        _environment = new ScriptEnvironment(
            new Script(Guid.NewGuid(), "Test", new ScriptConfig(ScriptKind.Program, GlobalConsts.AppDotNetFrameworkVersion)),
            _serviceProvider.CreateScope());

        _locator.Setup(l => l.GetServerLocationAsync()).ReturnsAsync(new OmniSharpServerLocation(_executablePath));

        _serverFactory
            .Setup(f => f.CreateStdioServerFromNewProcess(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<OmniSharpDotNetSdk?>()))
            .Returns((string _, string _, string? args, OmniSharpDotNetSdk? dotNetSdk) =>
            {
                var process = new FakeOmniSharpProcess(args, dotNetSdk);
                _processes.Add(process);
                return process.Server;
            });

        var tempDir = Path.GetTempPath();
        _settings = new Settings(tempDir, tempDir);

        _server = CreateServer(new CodeToolingLanguage(_settings));
    }

    [Fact]
    public async Task ReadyEvent_IsPublished_WhenProjectLoads()
    {
        await StartProcessAsync();

        await _processes[0].RaiseAsync(EventTypes.ProjectAdded);

        var ev = await NextReadyEventAsync();
        Assert.Equal(_environment.Script.Id, ev.ScriptId);
    }

    [Fact]
    public async Task ReadyEvent_IsNotPublished_BeforeProjectLoads()
    {
        await StartProcessAsync();

        await AssertNoReadyEventAsync();
    }

    [Fact]
    public async Task ReadyEvent_IsPublishedAgain_AfterRestart()
    {
        await StartProcessAsync();
        await _processes[0].RaiseAsync(EventTypes.ProjectAdded);
        await NextReadyEventAsync();

        await _server.RestartAsync();
        await _processes[1].RaiseAsync(EventTypes.ProjectChanged);

        var ev = await NextReadyEventAsync();
        Assert.Equal(_environment.Script.Id, ev.ScriptId);
        await AssertNoReadyEventAsync();
    }

    [Fact]
    public async Task ReadyEvent_IsNotPublished_ForProcessStoppedBeforeProjectLoads()
    {
        await StartProcessAsync();
        await _server.RestartAsync();

        await _processes[0].RaiseAsync(EventTypes.ProjectAdded);
        await AssertNoReadyEventAsync();

        await _processes[1].RaiseAsync(EventTypes.ProjectAdded);
        await NextReadyEventAsync();
        await AssertNoReadyEventAsync();
    }

    [Fact]
    public async Task Start_PinsOmniSharpToTheScriptSdk()
    {
        var major = GlobalConsts.AppDotNetFrameworkVersion.GetMajorVersion();
        SetInstalledSdks($"{major}.0.100", $"{major}.0.402");

        await StartProcessAsync();

        Assert.Equal($"{major}.0.402", _processes[0].DotNetSdk?.Version);
        Assert.Equal(_dotNetRoot, _processes[0].DotNetSdk?.RootDirectoryPath);
    }

    [Fact]
    public async Task Start_DoesNotPinAnSdk_WhenNoSdkOfTheScriptTargetIsFound()
    {
        var otherMajor = GlobalConsts.AppDotNetFrameworkVersion.GetMajorVersion() - 1;
        SetInstalledSdks($"{otherMajor}.0.100");
        _dotNetInfo.Setup(i => i.LocateDotNetRootDirectory()).Returns("/default/dotnet");

        await StartProcessAsync();

        Assert.Null(_processes[0].DotNetSdk?.Version);
        Assert.Equal("/default/dotnet", _processes[0].DotNetSdk?.RootDirectoryPath);
    }

    [Fact]
    public async Task Start_PassesNoSdk_WhenNoDotNetRootIsFound()
    {
        var otherMajor = GlobalConsts.AppDotNetFrameworkVersion.GetMajorVersion() - 1;
        SetInstalledSdks($"{otherMajor}.0.100");

        await StartProcessAsync();

        Assert.Null(_processes[0].DotNetSdk);
    }

    [Fact]
    public async Task Start_FailsWithTheRuntimeToInstall_WhenTheRuntimeOmniSharpNeedsIsMissingFromTheScriptSdkRoot()
    {
        SetInstalledSdks($"{GlobalConsts.AppDotNetFrameworkVersion.GetMajorVersion()}.0.100");
        SetInstalledNetCoreRuntimes(("8.0.22", _dotNetRoot), ("10.0.1", "/other/dotnet"));
        WriteRuntimeConfigBesideExecutable(requiredVersion: "10.0.0", rollForward: "LatestMajor");

        var ex = await Assert.ThrowsAsync<OmniSharpRuntimeMissingException>(() => _server.RestartAsync());

        Assert.Equal(new OmniSharpRuntimeRequirement(10, AllowsNewerMajorVersion: true), ex.Requirement);
        Assert.Empty(_processes);
    }

    [Fact]
    public async Task Start_StartsOmniSharp_WhenANewerRuntimeThanItNeedsIsInstalledAndRollForwardAllowsIt()
    {
        SetInstalledSdks($"{GlobalConsts.AppDotNetFrameworkVersion.GetMajorVersion()}.0.100");
        SetInstalledNetCoreRuntimes(("11.0.0", _dotNetRoot));
        WriteRuntimeConfigBesideExecutable(requiredVersion: "10.0.0", rollForward: "LatestMajor");

        await StartProcessAsync();

        Assert.Single(_processes);
    }

    [Fact]
    public async Task Start_StartsOmniSharp_WhenNoRuntimeConfigIsBesideTheExecutable()
    {
        SetInstalledNetCoreRuntimes(("8.0.22", _dotNetRoot));

        await StartProcessAsync();

        Assert.Single(_processes);
    }

    [Fact]
    public async Task Start_AsksForEnglish_WhenCodeToolingUsesEnglish()
    {
        using var restore = new ProcessUiLanguageRestorer();
        _settings.Localization.SetUseEnglishForCodeTooling(true);
        var language = new CodeToolingLanguage(_settings);
        language.ApplyToCurrentProcess();
        var server = CreateServer(language);

        await server.RestartAsync();

        Assert.EndsWith("--locale en", _processes[0].Args);
    }

    [Fact]
    public async Task Start_LeavesTheLanguageToTheSystem_WhenCodeToolingDoesNotUseEnglish()
    {
        await StartProcessAsync();

        Assert.DoesNotContain("--locale", _processes[0].Args);
    }

    [Fact]
    public async Task Start_LeavesTheLanguageToTheSystem_WhenEnglishWasNotAppliedToTheProcess()
    {
        _settings.Localization.SetUseEnglishForCodeTooling(true);
        var server = CreateServer(new CodeToolingLanguage(_settings));

        await server.RestartAsync();

        Assert.DoesNotContain("--locale", _processes[0].Args);
    }

    [Fact]
    public async Task Start_KeepsTheLanguageTheAppStartedWith_WhenTheSettingChangesLater()
    {
        var startupLanguage = new CodeToolingLanguage(_settings);
        startupLanguage.ApplyToCurrentProcess();
        _settings.Localization.SetUseEnglishForCodeTooling(true);
        var server = CreateServer(startupLanguage);

        await server.RestartAsync();

        Assert.DoesNotContain("--locale", _processes[0].Args);
    }

    private AppOmniSharpServer CreateServer(CodeToolingLanguage codeToolingLanguage) => new(
        _environment,
        _serverFactory.Object,
        _locator.Object,
        Mock.Of<IDataConnectionResourcesCache>(),
        Mock.Of<IScriptDependencyResolver>(),
        _settings,
        codeToolingLanguage,
        Mock.Of<ICodeParser>(),
        _serviceProvider.GetRequiredService<IEventBus>(),
        _dotNetInfo.Object,
        NullLogger<AppOmniSharpServer>.Instance,
        NullLogger<OmniSharpProject>.Instance);

    private void SetInstalledSdks(params string[] versions)
    {
        foreach (var version in versions)
        {
            Directory.CreateDirectory(Path.Combine(_dotNetRoot, "sdk", version));
        }

        _dotNetInfo
            .Setup(i => i.GetDotNetSdkVersions())
            .Returns(versions.Select(v => new DotNetSdkVersion(new SemanticVersion(v), _dotNetRoot)).ToArray());
    }

    private void SetInstalledNetCoreRuntimes(params (string Version, string DotNetRoot)[] runtimes)
    {
        _dotNetInfo
            .Setup(i => i.GetDotNetRuntimeVersions())
            .Returns(runtimes
                .Select(r => new DotNetRuntimeVersion(
                    DotNetRuntimeVersion.NetCoreFrameworkName,
                    new SemanticVersion(r.Version),
                    r.DotNetRoot))
                .ToArray());
    }

    private void WriteRuntimeConfigBesideExecutable(string requiredVersion, string rollForward)
    {
        File.WriteAllText(_runtimeConfigPath, $$"""
            {
              "runtimeOptions": {
                "rollForward": "{{rollForward}}",
                "framework": { "name": "Microsoft.NETCore.App", "version": "{{requiredVersion}}" }
              }
            }
            """);
    }

    /// <summary>
    /// Starts the OmniSharp process. RestartAsync does that without StartAsync's project setup, which
    /// runs a real dotnet restore.
    /// </summary>
    private Task StartProcessAsync() => _server.RestartAsync();

    private async Task<OmniSharpServerReadyEvent> NextReadyEventAsync()
    {
        using var cts = new CancellationTokenSource(_eventTimeout);
        return await _readyEvents.Reader.ReadAsync(cts.Token);
    }

    private async Task AssertNoReadyEventAsync()
    {
        await Task.Delay(_noEventGracePeriod);
        Assert.False(_readyEvents.Reader.TryRead(out _));
    }

    public void Dispose()
    {
        _environment.Dispose();
        _serviceProvider.Dispose();
        File.Delete(_executablePath);
        File.Delete(_runtimeConfigPath);
        Directory.Delete(_dotNetRoot, recursive: true);
    }

    /// <summary>
    /// Puts back the UI language that <see cref="CodeToolingLanguage.ApplyToCurrentProcess"/> changes for the
    /// whole process.
    /// </summary>
    private sealed class ProcessUiLanguageRestorer : IDisposable
    {
        private readonly CultureInfo? _defaultThreadUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;
        private readonly string? _cliUiLanguage = Environment.GetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE");

        public void Dispose()
        {
            CultureInfo.DefaultThreadCurrentUICulture = _defaultThreadUiCulture;
            CultureInfo.CurrentUICulture = _uiCulture;
            Environment.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", _cliUiLanguage);
        }
    }

    private sealed class FakeOmniSharpProcess
    {
        private readonly List<(string EventType, Func<JsonNode, Task> Handler)> _subscriptions = [];

        public FakeOmniSharpProcess(string? args, OmniSharpDotNetSdk? dotNetSdk)
        {
            Args = args ?? string.Empty;
            DotNetSdk = dotNetSdk;

            var server = new Mock<IOmniSharpStdioServer>();
            server.Setup(s => s.IsProcessRunning()).Returns(true);
            server
                .Setup(s => s.SubscribeToEvent(It.IsAny<string>(), It.IsAny<Func<JsonNode, Task>>()))
                .Callback<string, Func<JsonNode, Task>>((eventType, handler) => _subscriptions.Add((eventType, handler)));

            Server = server.Object;
        }

        public IOmniSharpStdioServer Server { get; }

        public string Args { get; }

        public OmniSharpDotNetSdk? DotNetSdk { get; }

        public Task RaiseAsync(string eventType) => Task.WhenAll(_subscriptions
            .Where(s => s.EventType == eventType)
            .Select(s => s.Handler(new JsonObject())));
    }
}
