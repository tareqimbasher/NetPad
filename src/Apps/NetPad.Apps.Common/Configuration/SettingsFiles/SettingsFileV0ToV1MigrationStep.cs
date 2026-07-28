using System.Text.Json.Nodes;
using NetPad.Common;
using NetPad.Configuration;

namespace NetPad.Apps.Configuration.SettingsFiles;

/// <summary>
/// Migrates a settings file from the shape that predates schema versioning (v0) to v1: appearance
/// settings and keyboard shortcuts.
/// </summary>
public class SettingsFileV0ToV1MigrationStep : IJsonMigrationStep
{
    public int FromVersion => 0;
    public int ToVersion => 1;

    public void Apply(JsonObject doc)
    {
        if (doc["appearance"] is JsonObject appearance)
        {
            MigrateTheme(appearance);
            MigrateExplorerRunIndicators(appearance);
            appearance.Remove("iconTheme");
        }

        if (doc["results"] is JsonObject results)
        {
            results.Remove("font");
        }

        if (doc["keyboardShortcuts"]?["shortcuts"] is JsonArray shortcuts)
        {
            foreach (var shortcut in shortcuts.OfType<JsonObject>())
            {
                MigrateShortcutId(shortcut);
                MigrateShortcutModifiers(shortcut);
                MigrateShortcutKey(shortcut);
            }
        }

        doc["version"] = ToVersion;
    }

    private static void MigrateTheme(JsonObject appearance)
    {
        if (!appearance.TryGetPropertyValue("theme", out var theme))
        {
            return;
        }

        appearance.Remove("theme");

        if (appearance.ContainsKey("mode"))
        {
            return;
        }

        // Before System mode existed the only choices were the two themes (Dark and Light), which
        // map to the new ThemeMode.
        if (theme is JsonValue value
         && value.TryGetValue<string>(out var name)
         && Enum.TryParse<ThemeMode>(name, true, out var mode)
         && mode != ThemeMode.System)
        {
            appearance["mode"] = mode.ToString();
        }
    }

    private static void MigrateExplorerRunIndicators(JsonObject appearance)
    {
        var showStatus = ReadBool(appearance, "showScriptRunStatusIndicatorInScriptsList");
        var showRunning = ReadBool(appearance, "showScriptRunningIndicatorInScriptsList");

        appearance.Remove("showScriptRunStatusIndicatorInScriptsList");
        appearance.Remove("showScriptRunningIndicatorInScriptsList");

        if ((showStatus is null && showRunning is null) || appearance.ContainsKey("scriptRunStatusIndicatorInExplorer"))
        {
            return;
        }

        // The two booleans covered statuses and the running state separately. Showing
        // statuses is the broader of the two, so it maps to Always.
        var visibility =
            showStatus == true ? StatusIndicatorVisibility.Always
            : showRunning == true ? StatusIndicatorVisibility.WhileRunning
            : StatusIndicatorVisibility.Off;

        appearance["scriptRunStatusIndicatorInExplorer"] = visibility.ToString();
    }

    private static void MigrateShortcutId(JsonObject shortcut)
    {
        if (shortcut.TryGetPropertyValue("id", out var node)
            && node is JsonValue value
            && value.TryGetValue<string>(out var id)
            && _renamedShortcutIds.TryGetValue(id, out var commandId))
        {
            shortcut["id"] = commandId;
        }
    }

    private static void MigrateShortcutModifiers(JsonObject shortcut)
    {
        if (!shortcut.TryGetPropertyValue("ctrl", out var ctrl))
        {
            return;
        }

        shortcut.Remove("ctrl");

        if (!shortcut.ContainsKey("primary") && ctrl is JsonValue value && value.TryGetValue<bool>(out var pressed))
        {
            shortcut["primary"] = pressed;
        }
    }

    private static void MigrateShortcutKey(JsonObject shortcut)
    {
        if (!shortcut.TryGetPropertyValue("key", out var node)
            || node is not JsonValue value
            || !value.TryGetValue<string>(out var keyCode)
            || string.IsNullOrWhiteSpace(keyCode))
        {
            return;
        }

        shortcut["key"] = ToLogicalKey(keyCode);
    }

    private static string? ToLogicalKey(string keyCode)
    {
        if (_unusableKeyCodes.Contains(keyCode))
        {
            return null;
        }

        if (_renamedKeyCodes.TryGetValue(keyCode, out var key))
        {
            return key;
        }

        if (keyCode.Length == 4 && keyCode.StartsWith("Key", StringComparison.Ordinal))
        {
            return keyCode[3..].ToUpperInvariant();
        }

        if (keyCode.Length == 6 && keyCode.StartsWith("Digit", StringComparison.Ordinal))
        {
            return keyCode[5..];
        }

        if (keyCode.Length == 7 && keyCode.StartsWith("Numpad", StringComparison.Ordinal))
        {
            return keyCode[6..];
        }

        return keyCode;
    }

    private static bool? ReadBool(JsonObject obj, string key)
    {
        return obj.TryGetPropertyValue(key, out var node)
            && node is JsonValue value
            && value.TryGetValue<bool>(out var parsed)
            ? parsed
            : null;
    }

    /// <summary>
    /// The ids keyboard shortcuts were saved under before commands existed, and the command each
    /// one now names.
    /// </summary>
    private static readonly Dictionary<string, string> _renamedShortcutIds = new()
    {
        ["shortcut.commandpalette.open"] = "view.commandPalette",
        ["shortcut.documents.quickopen"] = "file.goToScript",
        ["shortcut.documents.switchtolastactive"] = "script.switchToLastActive",
        ["shortcut.documents.new"] = "file.new",
        ["shortcut.documents.open"] = "file.open",
        ["shortcut.documents.close"] = "file.close",
        ["shortcut.documents.save"] = "file.save",
        ["shortcut.documents.saveall"] = "file.saveAll",
        ["shortcut.documents.run"] = "script.run",
        ["shortcut.documents.properties"] = "file.properties",
        ["shortcut.settings.open"] = "file.settings",
        ["shortcut.output.open"] = "view.output",
        ["shortcut.explorer.open"] = "view.explorer",
        ["shortcut.namespaces.open"] = "view.namespaces",
        ["shortcut.window.reload"] = "view.reload",
        ["shortcut.window.zoomIn"] = "view.zoomIn",
        ["shortcut.window.zoomOut"] = "view.zoomOut",
        ["shortcut.window.zoomReset"] = "view.resetZoom",
        ["shortcut.editor.vim.toggle"] = "view.toggleVimMode",
    };

    /// <summary>
    /// The <c>KeyCode</c>s whose name differs from the key they produce on a US layout. Codes not
    /// listed here already name their key ("Tab", "F5") and are left as they are.
    /// </summary>
    private static readonly Dictionary<string, string> _renamedKeyCodes = new()
    {
        ["NumpadMultiply"] = "*",
        ["NumpadAdd"] = "+",
        ["NumpadSubtract"] = "-",
        ["NumpadDecimal"] = ".",
        ["NumpadDivide"] = "/",
        ["Semicolon"] = ";",
        ["Equal"] = "=",
        ["Comma"] = ",",
        ["Minus"] = "-",
        ["Period"] = ".",
        ["Slash"] = "/",
        ["Backquote"] = "`",
        ["BracketLeft"] = "[",
        ["Backslash"] = "\\",
        ["BracketRight"] = "]",
        ["Quote"] = "'",
    };

    /// <summary>
    /// The <c>KeyCode</c>s a combination cannot end on. A shortcut holding one could never have
    /// fired, so its key is dropped.
    /// </summary>
    private static readonly HashSet<string> _unusableKeyCodes =
    [
        "Unknown",
        "ShiftLeft",
        "ShiftRight",
        "ControlLeft",
        "ControlRight",
        "AltLeft",
        "AltRight",
        "MetaLeft",
        "MetaRight",
        "CapsLock",
        "NumLock",
        "ScrollLock",
    ];
}
