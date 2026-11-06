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
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(() =>
            {
                var process = new FakeOmniSharpProcess();
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
            Mock.Of<IDotNetInfo>(),
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
    }

    private sealed class FakeOmniSharpProcess
    {
        private readonly List<(string EventType, Func<JsonNode, Task> Handler)> _subscriptions = [];

        public FakeOmniSharpProcess()
        {
            var server = new Mock<IOmniSharpStdioServer>();
            server.Setup(s => s.IsProcessRunning()).Returns(true);
            server
                .Setup(s => s.SubscribeToEvent(It.IsAny<string>(), It.IsAny<Func<JsonNode, Task>>()))
                .Callback<string, Func<JsonNode, Task>>((eventType, handler) => _subscriptions.Add((eventType, handler)));

            Server = server.Object;
        }

        public IOmniSharpStdioServer Server { get; }

        public Task RaiseAsync(string eventType) => Task.WhenAll(_subscriptions
            .Where(s => s.EventType == eventType)
            .Select(s => s.Handler(new JsonObject())));
    }
}
