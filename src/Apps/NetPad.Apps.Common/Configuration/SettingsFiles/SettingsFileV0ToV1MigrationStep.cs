using System.Text.Json.Nodes;
using NetPad.Common;
using NetPad.Configuration;

namespace NetPad.Apps.Configuration.SettingsFiles;

/// <summary>
/// Migrates a settings file from the shape that predates schema versioning (v0) to v1: folds the
/// appearance settings that were replaced onto the properties that replaced them, and drops the ones
/// that were retired without a successor.
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

    private static bool? ReadBool(JsonObject obj, string key)
    {
        return obj.TryGetPropertyValue(key, out var node)
            && node is JsonValue value
            && value.TryGetValue<bool>(out var parsed)
            ? parsed
            : null;
    }
}
