using System.Text.Json;
using NetPad.Apps.UiInterop;
using NetPad.Events;
using NetPad.Plugins.OmniSharp.Events;

namespace NetPad.Plugins.OmniSharp.BackgroundServices;

public class DiagnosticsEventsBackgroundService(IIpcService ipcService, IEventBus eventBus) : IHostedService
{
    private readonly List<IDisposable> _disposables = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var serverStartSubscription = eventBus.Subscribe<OmniSharpServerStartedEvent>(ev =>
        {
            var server = ev.AppOmniSharpServer;
            var scriptId = server.ScriptId;

            server.OmniSharpServer.SubscribeToEvent("Diagnostic", async node =>
            {
                var body = node["Body"];

                var diagnostics = body?.Deserialize<OmniSharpDiagnosticMessage>();
                if (diagnostics == null)
                    return;

                // Only send diagnostics for user program
                var results = diagnostics.Results
                    .Where(d => d.FileName == server.Project.UserProgramFilePath)
                    .ToArray();

                if (results.Length == 0)
                {
                    return;
                }

                diagnostics.Results = results;

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
