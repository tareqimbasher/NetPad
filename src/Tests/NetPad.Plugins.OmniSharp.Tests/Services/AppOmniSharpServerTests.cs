using System.Text.Json.Nodes;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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

public sealed class AppOmniSharpServerTests : IDisposable
{
    private static readonly TimeSpan _eventTimeout = TimeSpan.FromSeconds(10);

    /// <summary>How long to wait before concluding that an event will not be published.</summary>
    private static readonly TimeSpan _noEventGracePeriod = TimeSpan.FromMilliseconds(500);

    private readonly string _executablePath = Path.GetTempFileName();
    private readonly string _dotNetRoot = Directory.CreateTempSubdirectory().FullName;
    private readonly Mock<IDotNetInfo> _dotNetInfo = new();
    private readonly ServiceProvider _serviceProvider;
    private readonly ScriptEnvironment _environment;
    private readonly List<FakeOmniSharpProcess> _processes = [];
    private readonly Channel<OmniSharpServerReadyEvent> _readyEvents = Channel.CreateUnbounded<OmniSharpServerReadyEvent>();
    private readonly AppOmniSharpServer _server;

    public AppOmniSharpServerTests()
    {
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

        var locator = new Mock<IOmniSharpServerLocator>();
        locator.Setup(l => l.GetServerLocationAsync()).ReturnsAsync(new OmniSharpServerLocation(_executablePath));

        var serverFactory = new Mock<IOmniSharpServerFactory>();
        serverFactory
            .Setup(f => f.CreateStdioServerFromNewProcess(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<OmniSharpDotNetSdk?>()))
            .Returns((string _, string _, string? _, OmniSharpDotNetSdk? dotNetSdk) =>
            {
                var process = new FakeOmniSharpProcess(dotNetSdk);
                _processes.Add(process);
                return process.Server;
            });

        var tempDir = Path.GetTempPath();

        _server = new AppOmniSharpServer(
            _environment,
            serverFactory.Object,
            locator.Object,
            Mock.Of<IDataConnectionResourcesCache>(),
            Mock.Of<IScriptDependencyResolver>(),
            new Settings(tempDir, tempDir),
            Mock.Of<ICodeParser>(),
            eventBus,
            _dotNetInfo.Object,
            NullLogger<AppOmniSharpServer>.Instance,
            NullLogger<OmniSharpProject>.Instance);
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
        Directory.Delete(_dotNetRoot, recursive: true);
    }

    private sealed class FakeOmniSharpProcess
    {
        private readonly List<(string EventType, Func<JsonNode, Task> Handler)> _subscriptions = [];

        public FakeOmniSharpProcess(OmniSharpDotNetSdk? dotNetSdk)
        {
            DotNetSdk = dotNetSdk;

            var server = new Mock<IOmniSharpStdioServer>();
            server.Setup(s => s.IsProcessRunning()).Returns(true);
            server
                .Setup(s => s.SubscribeToEvent(It.IsAny<string>(), It.IsAny<Func<JsonNode, Task>>()))
                .Callback<string, Func<JsonNode, Task>>((eventType, handler) => _subscriptions.Add((eventType, handler)));

            Server = server.Object;
        }

        public IOmniSharpStdioServer Server { get; }

        public OmniSharpDotNetSdk? DotNetSdk { get; }

        public Task RaiseAsync(string eventType) => Task.WhenAll(_subscriptions
            .Where(s => s.EventType == eventType)
            .Select(s => s.Handler(new JsonObject())));
    }
}
