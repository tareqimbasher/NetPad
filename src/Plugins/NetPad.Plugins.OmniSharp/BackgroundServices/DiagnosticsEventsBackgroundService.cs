using System.Text.Json;
using NetPad.Application;
using NetPad.Events;
using NetPad.Plugins.OmniSharp.Events;
using NetPad.UiInterop;
using OmniSharp.Models.Events;

namespace NetPad.Plugins.OmniSharp.BackgroundServices;

public class DiagnosticsEventsBackgroundService : IHostedService
{
    private readonly IAppStatusMessagePublisher _appStatusMessagePublisher;
    private readonly IIpcService _ipcService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<DiagnosticsEventsBackgroundService> _logger;
    private readonly List<IDisposable> _disposables;

    public DiagnosticsEventsBackgroundService(
        IAppStatusMessagePublisher appStatusMessagePublisher,
        IIpcService ipcService,
        IEventBus eventBus,
        ILogger<DiagnosticsEventsBackgroundService> logger)
    {
        _appStatusMessagePublisher = appStatusMessagePublisher;
        _ipcService = ipcService;
        _eventBus = eventBus;
        _logger = logger;
        _disposables = new List<IDisposable>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var serverStartSubscription = _eventBus.Subscribe<OmniSharpServerStartedEvent>(ev =>
        {
            var server = ev.AppOmniSharpServer;
            var scriptId = server.ScriptId;

            server.OmniSharpServer.SubscribeToEvent("BackgroundDiagnosticStatus", async node =>
            {
                var body = node["Body"];

                var diagnostic = body?.Deserialize<OmniSharpBackgroundDiagnostic>();
                if (diagnostic == null)
                    return;

                string message = diagnostic.Status == BackgroundDiagnosticStatus.Finished
                    ? $"OmniSharp analyzed {diagnostic.NumberFilesTotal} files"
                    : $"OmniSharp analyzing {diagnostic.NumberFilesTotal - diagnostic.NumberFilesRemaining}/{diagnostic.NumberFilesTotal} files";

                await _appStatusMessagePublisher.PublishAsync(scriptId, message);
            });

            server.OmniSharpServer.SubscribeToEvent("Diagnostic", async node =>
            {
                var body = node["Body"];

                var diagnostics = body?.Deserialize<OmniSharpDiagnosticMessage>();
                if (diagnostics == null)
                    return;

                // Only send diagnostics for user program
                diagnostics.Results = diagnostics.Results
                    .Where(d => d.FileName == server.Project.UserProgramFilePath)
                    .ToArray();

                if (!diagnostics.Results.Any())
                    return;

                await _ipcService.SendAsync(new OmniSharpDiagnosticsEvent(scriptId, diagnostics));
            });

            return Task.CompletedTask;
        });

        _disposables.Add(serverStartSubscription);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        return Task.CompletedTask;
    }
}
