using NetPad.Scripts;

namespace NetPad.UiInterop;

public interface IUiWindowService
{
    Task MaximizeMainWindowAsync();
    Task MinimizeMainWindowAsync();
    Task ToggleAlwaysOnTopMainWindowAsync();
    Task OpenMainWindowAsync();
    Task OpenSettingsWindowAsync(string? tab = null);
    Task OpenScriptConfigWindowAsync(Script script, string? tab = null);
    Task OpenDataConnectionWindowAsync(Guid? dataConnectionId);
}
