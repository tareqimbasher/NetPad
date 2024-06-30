using System.Text.Json;
using NetPad.Application;
using NetPad.Apps.UiInterop;
using NetPad.Events;
using NetPad.Plugins.OmniSharp.Events;
using OmniSharp.Models.Events;

namespace NetPad.Plugins.OmniSharp.BackgroundServices;

public class DiagnosticsEventsBackgroundService(
    IAppStatusMessagePublisher appStatusMessagePublisher,
    IIpcService ipcService,
    IEventBus eventBus,
    ILogger<DiagnosticsEventsBackgroundService> logger)
    : IHostedService
{
    private readonly ILogger<DiagnosticsEventsBackgroundService> _logger = logger;
    private readonly List<IDisposable> _disposables = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var serverStartSubscription = eventBus.Subscribe<OmniSharpServerStartedEvent>(ev =>
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

                await appStatusMessagePublisher.PublishAsync(scriptId, message);
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

                await ipcService.SendAsync(new OmniSharpDiagnosticsEvent(scriptId, diagnostics));
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
