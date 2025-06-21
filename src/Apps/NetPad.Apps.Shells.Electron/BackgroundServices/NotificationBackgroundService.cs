using ElectronSharp.API.Entities;
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
            if (await ElectronUtil.MainWindow.IsFocusedAsync() != true)
            {
                var environment = ev.ScriptEnvironment;

                var status = environment.Status;
                string message;

                if (status == ScriptStatus.Error)
                {
                    message = $"\"{environment.Script.Name}\" finished with errors";
                }
                else if (status == ScriptStatus.Ready && environment.RunDurationMilliseconds != null)
                {
                    message = $"\"{environment.Script.Name}\" finished successfully (took: {environment.RunDurationMilliseconds} ms)";
                }
                else
                {
                    return;
                }

                ElectronSharp.API.Electron.Notification.Show(new NotificationOptions("NetPad", message)
                {
                    Icon = logoService.GetLogoPath(LogoStyle.Circle, LogoSize._64)
                });
            }
        });

        return Task.CompletedTask;
    }
}
