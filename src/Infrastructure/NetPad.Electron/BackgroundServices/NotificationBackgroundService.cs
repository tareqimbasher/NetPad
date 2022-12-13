using ElectronNET.API.Entities;
using Microsoft.Extensions.Hosting;
using NetPad.Electron.UiInterop;
using NetPad.Events;
using NetPad.Resources;
using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.Electron.BackgroundServices;

public class NotificationBackgroundService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly ILogoService _logoService;

    public NotificationBackgroundService(IEventBus eventBus, ILogoService logoService)
    {
        _eventBus = eventBus;
        _logoService = logoService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _eventBus.Subscribe<ScriptRanEvent>(async ev =>
        {
            if (!await ElectronUtil.MainWindow.IsFocusedAsync())
            {
                var environment = ev.ScriptEnvironment;
                string status = environment.Status == ScriptStatus.Ready ? "successfully" : "with failures";
                string message = $"\"{environment.Script.Name}\" finished {status} (took: {environment.RunDurationMilliseconds} ms)";

                ElectronNET.API.Electron.Notification.Show(new NotificationOptions("NetPad", message)
                {
                    Icon = _logoService.GetLogoPath(LogoStyle.Circle, LogoSize._64)
                });
            }
        });

        return Task.CompletedTask;
    }
}
