using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetPad.Apps.UiInterop;
using NetPad.Common;
using NetPad.Events;
using NetPad.ExecutionModel;
using NetPad.Presentation;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Services;

namespace NetPad.Apps.App.Tests.Services;

public sealed class ScriptEnvironmentIpcOutputWriterTests : IDisposable
{
    private static readonly TimeSpan _messageTimeout = TimeSpan.FromSeconds(10);

    /// <summary>How long to wait before concluding that a message will not be sent.</summary>
    private static readonly TimeSpan _noMessageGracePeriod = TimeSpan.FromMilliseconds(500);

    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _serviceScope;
    private readonly IEventBus _eventBus;
    private readonly ScriptEnvironment _environment;
    private readonly ScriptEnvironmentIpcOutputWriter _writer;
    private readonly List<IpcMessage> _sentMessages = [];
    private readonly Lock _sentMessagesLock = new();
    private readonly TaskCompletionSource _messageSent = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ScriptEnvironmentIpcOutputWriterTests()
    {
        var runnerFactory = new Mock<IScriptRunnerFactory>();
        runnerFactory.Setup(f => f.CreateRunner(It.IsAny<Script>())).Returns(Mock.Of<IScriptRunner>());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton(runnerFactory.Object);

        _serviceProvider = services.BuildServiceProvider();
        _serviceScope = _serviceProvider.CreateScope();
        _eventBus = _serviceProvider.GetRequiredService<IEventBus>();

        _environment = new ScriptEnvironment(
            new Script(Guid.NewGuid(), "Test", new ScriptConfig(ScriptKind.Program, GlobalConsts.AppDotNetFrameworkVersion)),
            _serviceScope);

        var ipcService = new Mock<IIpcService>();
        ipcService
            .Setup(s => s.SendAsync(It.IsAny<IpcMessageBatch>(), It.IsAny<CancellationToken>()))
            .Callback<IpcMessageBatch, CancellationToken>((batch, _) =>
            {
                lock (_sentMessagesLock)
                {
                    _sentMessages.AddRange(batch.Messages);
                }

                _messageSent.TrySetResult();
            })
            .Returns(Task.CompletedTask);

        _writer = new ScriptEnvironmentIpcOutputWriter(
            _environment,
            ipcService.Object,
            _eventBus,
            NullLogger<ScriptEnvironmentIpcOutputWriter>.Instance);
    }

    [Fact]
    public async Task SystemNotice_IsSentAfterScriptIsStopped()
    {
        await SetStatusAsync(ScriptStatus.Ready, ScriptStatus.Running);
        await SetStatusAsync(ScriptStatus.Running, ScriptStatus.Stopping);

        await _writer.WriteAsync(new ScriptOutput(ScriptOutputKind.Result, "Script stopped at: 12:00:00 AM"));

        var message = Assert.Single(await WaitForSentMessagesAsync());
        var emitted = Assert.IsType<ScriptOutputEmittedEvent>(message.Message);
        var body = emitted.Output.Body?.Replace("&nbsp;", " ");

        Assert.Contains("Script stopped at: 12:00:00 AM", body);
        Assert.Contains("raw", body);
    }

    [Fact]
    public async Task UserOutput_IsSentWhileScriptIsRunning()
    {
        await SetStatusAsync(ScriptStatus.Ready, ScriptStatus.Running);

        await _writer.WriteAsync(new ScriptOutput(ScriptOutputKind.Result, 0, "<span>output</span>", ScriptOutputFormat.Html));

        var message = Assert.Single(await WaitForSentMessagesAsync());
        var emitted = Assert.IsType<ScriptOutputEmittedEvent>(message.Message);
        Assert.Equal("<span>output</span>", emitted.Output.Body);
    }

    [Fact]
    public async Task UserOutput_IsNotSentAfterScriptIsStopped()
    {
        await SetStatusAsync(ScriptStatus.Ready, ScriptStatus.Running);
        await SetStatusAsync(ScriptStatus.Running, ScriptStatus.Stopping);

        await _writer.WriteAsync(new ScriptOutput(ScriptOutputKind.Result, 0, "<span>output</span>", ScriptOutputFormat.Html));

        await Task.Delay(_noMessageGracePeriod);

        Assert.Empty(GetSentMessages());
    }

    private async Task SetStatusAsync(ScriptStatus oldStatus, ScriptStatus newStatus)
    {
        await _eventBus.PublishAsync(new EnvironmentPropertyChangedEvent(
            _environment.Script.Id,
            nameof(ScriptEnvironment.Status),
            oldStatus,
            newStatus));
    }

    private async Task<IReadOnlyList<IpcMessage>> WaitForSentMessagesAsync()
    {
        var completed = await Task.WhenAny(_messageSent.Task, Task.Delay(_messageTimeout));

        Assert.True(completed == _messageSent.Task, "Timed out waiting for output to be sent to IPC clients.");

        return GetSentMessages();
    }

    private IReadOnlyList<IpcMessage> GetSentMessages()
    {
        lock (_sentMessagesLock)
        {
            return _sentMessages.ToArray();
        }
    }

    public void Dispose()
    {
        _writer.Dispose();
        _environment.Dispose();
        _serviceScope.Dispose();
        _serviceProvider.Dispose();
    }
}
