using ElectronNET.API.Entities;
using Microsoft.Extensions.Hosting;
using NetPad.Apps.Resources;
using NetPad.Apps.Shells.Electron.UiInterop;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.Apps.Shells.Electron.BackgroundServices;

public class NotificationBackgroundService(IEventBus eventBus, ILogoService logoService) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        eventBus.Subscribe<ScriptRanEvent>(async ev =>
        {
            if (!await ElectronUtil.MainWindow.IsFocusedAsync())
            {
                var environment = ev.ScriptEnvironment;
                string status = environment.Status == ScriptStatus.Ready ? "successfully" : "with failures";
                string message = $"\"{environment.Script.Name}\" finished {status} (took: {environment.RunDurationMilliseconds} ms)";

                ElectronNET.API.Electron.Notification.Show(new NotificationOptions("NetPad", message)
                {
                    Icon = logoService.GetLogoPath(LogoStyle.Circle, LogoSize._64)
                });
            }
        });

        return Task.CompletedTask;
    }
}
