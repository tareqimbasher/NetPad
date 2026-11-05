using NetPad.Apps.UiInterop;
using NetPad.Scripts;

namespace NetPad.Apps.CQs;

/// <summary>
/// Asks the user whether to save a script with unsaved changes.
/// </summary>
public class ConfirmSaveCommand(Script script) : Command<YesNoCancel>
{
    public string ScriptName { get; } = script.Name;
}
