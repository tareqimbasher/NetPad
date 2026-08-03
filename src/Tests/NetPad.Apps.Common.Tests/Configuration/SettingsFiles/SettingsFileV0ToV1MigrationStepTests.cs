using System.Text.Json.Nodes;
using NetPad.Apps.Configuration.SettingsFiles;
using NetPad.Common;
using NetPad.Configuration;

namespace NetPad.Apps.Common.Tests.Configuration.SettingsFiles;

public class SettingsFileV0ToV1MigrationStepTests
{
    [Fact]
    public void Migrates_A_Full_V0_File()
    {
        var doc = Parse("""
                        {
                          "version": 0,
                          "autoCheckUpdates": true,
                          "appearance": {
                            "theme": "Light",
                            "iconTheme": "Colorful",
                            "showScriptRunStatusIndicatorInTab": false,
                            "showScriptRunStatusIndicatorInScriptsList": true,
                            "showScriptRunningIndicatorInScriptsList": false,
                            "titlebar": { "type": "Native" }
                          },
                          "results": {
                            "font": "monospace",
                            "textWrap": true
                          }
                        }
                        """);

        Apply(doc);

        var appearance = (JsonObject)doc["appearance"]!;
        var results = (JsonObject)doc["results"]!;

        Assert.Equal(1, (int)doc["version"]!);
        Assert.Equal("Light", (string?)appearance["mode"]);
        Assert.Equal("Always", (string?)appearance["scriptRunStatusIndicatorInExplorer"]);

        Assert.False(appearance.ContainsKey("theme"));
        Assert.False(appearance.ContainsKey("iconTheme"));
        Assert.False(appearance.ContainsKey("showScriptRunStatusIndicatorInScriptsList"));
        Assert.False(appearance.ContainsKey("showScriptRunningIndicatorInScriptsList"));
        Assert.False(results.ContainsKey("font"));

        // Settings the migration has no business touching.
        Assert.True((bool)doc["autoCheckUpdates"]!);
        Assert.False((bool)appearance["showScriptRunStatusIndicatorInTab"]!);
        Assert.Equal("Native", (string?)appearance["titlebar"]!["type"]);
        Assert.True((bool)results["textWrap"]!);
    }

    [Fact]
    public void Migrated_File_Deserializes_Into_Settings()
    {
        var pipeline = new JsonMigrationPipeline([new SettingsFileV0ToV1MigrationStep()]);

        var settings = pipeline.MigrateToLatest<Settings>(
            """
            {
              "appearance": {
                "theme": "Dark",
                "showScriptRunningIndicatorInScriptsList": true
              }
            }
            """,
            JsonSerializer.DefaultOptions);

        Assert.Equal(1, settings.Version);
        Assert.Equal(ThemeMode.Dark, settings.Appearance.Mode);
        Assert.Equal(StatusIndicatorVisibility.WhileRunning, settings.Appearance.ScriptRunStatusIndicatorInExplorer);
    }

    [Theory]
    [InlineData("Dark", "Dark")]
    [InlineData("Light", "Light")]
    [InlineData("dark", "Dark")]
    [InlineData("LIGHT", "Light")]
    public void Theme_Maps_Onto_Mode(string theme, string expected)
    {
        var doc = ParseAppearance($$"""{"theme": "{{theme}}"}""");

        Apply(doc);

        Assert.Equal(expected, (string?)Appearance(doc)["mode"]);
    }

    [Theory]
    [InlineData("\"Twilight\"")]
    [InlineData("\"System\"")]
    [InlineData("null")]
    [InlineData("7")]
    public void Unmappable_Theme_Is_Dropped_Without_Setting_A_Mode(string theme)
    {
        var doc = ParseAppearance($$"""{"theme": {{theme}}}""");

        Apply(doc);

        Assert.False(Appearance(doc).ContainsKey("theme"));
        Assert.False(Appearance(doc).ContainsKey("mode"));
    }

    [Theory]
    [InlineData(true, true, "Always")]
    [InlineData(true, false, "Always")]
    [InlineData(false, true, "WhileRunning")]
    [InlineData(false, false, "Off")]
    public void Explorer_Indicator_Booleans_Map_Onto_The_Tri_State(bool showStatus, bool showRunning, string expected)
    {
        var doc = ParseAppearance($$"""
                                    {
                                      "showScriptRunStatusIndicatorInScriptsList": {{Json(showStatus)}},
                                      "showScriptRunningIndicatorInScriptsList": {{Json(showRunning)}}
                                    }
                                    """);

        Apply(doc);

        Assert.Equal(expected, (string?)Appearance(doc)["scriptRunStatusIndicatorInExplorer"]);
    }

    [Fact]
    public void One_Explorer_Indicator_Boolean_Is_Enough_To_Fold()
    {
        var doc = ParseAppearance("""{"showScriptRunningIndicatorInScriptsList": true}""");

        Apply(doc);

        Assert.Equal("WhileRunning", (string?)Appearance(doc)["scriptRunStatusIndicatorInExplorer"]);
    }

    [Fact]
    public void Absent_Explorer_Indicator_Booleans_Leave_The_Tri_State_Unset()
    {
        var doc = ParseAppearance("{}");

        Apply(doc);

        Assert.False(Appearance(doc).ContainsKey("scriptRunStatusIndicatorInExplorer"));
    }

    [Fact]
    public void Icon_Theme_Is_Dropped()
    {
        var doc = ParseAppearance("""{"iconTheme": "Colorful"}""");

        Apply(doc);

        Assert.False(Appearance(doc).ContainsKey("iconTheme"));
    }

    [Theory]
    [InlineData("\"netpad-dark-theme\"")]      // retired with the design refresh
    [InlineData("\"netpad-light-theme\"")]
    [InlineData("\"Dracula\"")]                // still a real theme; unpinned all the same
    [InlineData("null")]
    [InlineData("7")]
    public void Every_Editor_Theme_Is_Unpinned(string theme)
    {
        var doc = ParseMonacoOptions($$"""{"theme": {{theme}}, "wordWrap": "on"}""");

        Apply(doc);

        var monacoOptions = MonacoOptions(doc);
        Assert.False(monacoOptions.ContainsKey("theme"));
        Assert.Equal("on", (string?)monacoOptions["wordWrap"]);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"editor": null}""")]
    [InlineData("""{"editor": {"monacoOptions": null}}""")]
    [InlineData("""{"editor": {"monacoOptions": "not an object"}}""")]
    public void An_Absent_Or_Unexpected_Editor_Section_Is_Tolerated(string json)
    {
        var doc = Parse(json);

        Apply(doc);

        Assert.Equal(1, (int)doc["version"]!);
    }

    [Fact]
    public void Other_Editor_Options_Survive_An_Unpinning()
    {
        var doc = ParseMonacoOptions("""{"wordWrap": "on"}""");

        Apply(doc);

        var monacoOptions = MonacoOptions(doc);
        Assert.False(monacoOptions.ContainsKey("theme"));
        Assert.Equal("on", (string?)monacoOptions["wordWrap"]);
    }

    [Fact]
    public void Results_Font_Is_Dropped()
    {
        var doc = Parse("""{"results": {"font": "monospace", "openOnRun": true}}""");

        Apply(doc);

        var results = (JsonObject)doc["results"]!;
        Assert.False(results.ContainsKey("font"));
        Assert.True((bool)results["openOnRun"]!);
    }

    [Fact]
    public void A_File_Already_Carrying_The_V1_Shape_Passes_Through_Unharmed()
    {
        var doc = ParseAppearance("""
                                  {
                                    "theme": "Dark",
                                    "mode": "System",
                                    "showScriptRunStatusIndicatorInScriptsList": true,
                                    "scriptRunStatusIndicatorInExplorer": "Off"
                                  }
                                  """);

        Apply(doc);

        var appearance = Appearance(doc);
        Assert.Equal("System", (string?)appearance["mode"]);
        Assert.Equal("Off", (string?)appearance["scriptRunStatusIndicatorInExplorer"]);
        Assert.False(appearance.ContainsKey("theme"));
        Assert.False(appearance.ContainsKey("showScriptRunStatusIndicatorInScriptsList"));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"appearance": null}""")]
    [InlineData("""{"appearance": "not an object", "results": 4}""")]
    public void Absent_Or_Unexpected_Sections_Are_Tolerated(string json)
    {
        var doc = Parse(json);

        Apply(doc);

        Assert.Equal(1, (int)doc["version"]!);
    }

    [Fact]
    public void A_File_With_No_Version_Is_Treated_As_V0()
    {
        var pipeline = new JsonMigrationPipeline([new SettingsFileV0ToV1MigrationStep()]);

        var settings = pipeline.MigrateToLatest<Settings>(
            """{"appearance": {"theme": "Light"}}""",
            JsonSerializer.DefaultOptions);

        Assert.Equal(ThemeMode.Light, settings.Appearance.Mode);
    }

    [Fact]
    public void Retired_Keys_Are_Not_Written_Back()
    {
        var pipeline = new JsonMigrationPipeline([new SettingsFileV0ToV1MigrationStep()]);

        var settings = pipeline.MigrateToLatest<Settings>(
            """
            {
              "appearance": {
                "theme": "Light",
                "iconTheme": "Colorful",
                "showScriptRunStatusIndicatorInScriptsList": true,
                "showScriptRunningIndicatorInScriptsList": true
              },
              "results": { "font": "monospace" }
            }
            """,
            JsonSerializer.DefaultOptions);

        var json = JsonSerializer.Serialize(settings);

        Assert.DoesNotContain("\"theme\"", json);
        Assert.DoesNotContain("iconTheme", json);
        Assert.DoesNotContain("IndicatorInScriptsList", json);
        Assert.DoesNotContain("\"font\"", json);
        Assert.Contains("\"mode\":\"Light\"", json);
        Assert.Contains("\"scriptRunStatusIndicatorInExplorer\":\"Always\"", json);
        Assert.Contains("\"version\":1", json);
    }

    [Fact]
    public void Migrates_Keyboard_Shortcuts()
    {
        var doc = Parse("""
                        {
                          "keyboardShortcuts": {
                            "shortcuts": [
                              { "id": "shortcut.documents.save", "meta": false, "alt": false, "ctrl": true, "shift": true, "key": "KeyS" },
                              { "id": "shortcut.namespaces.open", "meta": false, "alt": true, "ctrl": false, "shift": false, "key": "KeyN" }
                            ]
                          }
                        }
                        """);

        Apply(doc);

        var first = Shortcut(doc, 0);
        Assert.Equal("file.save", (string?)first["id"]);
        Assert.True((bool)first["primary"]!);
        Assert.True((bool)first["shift"]!);
        Assert.Equal("S", (string?)first["key"]);
        Assert.False(first.ContainsKey("ctrl"));

        var second = Shortcut(doc, 1);
        Assert.Equal("view.namespaces", (string?)second["id"]);
        Assert.False((bool)second["primary"]!);
        Assert.True((bool)second["alt"]!);
        Assert.Equal("N", (string?)second["key"]);
    }

    [Theory]
    [InlineData("shortcut.commandpalette.open", "view.commandPalette")]
    [InlineData("shortcut.documents.quickopen", "file.goToScript")]
    [InlineData("shortcut.documents.switchtolastactive", "script.switchToLastActive")]
    [InlineData("shortcut.documents.new", "file.new")]
    [InlineData("shortcut.documents.open", "file.open")]
    [InlineData("shortcut.documents.close", "file.close")]
    [InlineData("shortcut.documents.save", "file.save")]
    [InlineData("shortcut.documents.saveall", "file.saveAll")]
    [InlineData("shortcut.documents.run", "script.run")]
    [InlineData("shortcut.documents.properties", "file.properties")]
    [InlineData("shortcut.settings.open", "file.settings")]
    [InlineData("shortcut.output.open", "view.output")]
    [InlineData("shortcut.explorer.open", "view.explorer")]
    [InlineData("shortcut.namespaces.open", "view.namespaces")]
    [InlineData("shortcut.window.reload", "view.reload")]
    [InlineData("shortcut.window.zoomIn", "view.zoomIn")]
    [InlineData("shortcut.window.zoomOut", "view.zoomOut")]
    [InlineData("shortcut.window.zoomReset", "view.resetZoom")]
    [InlineData("shortcut.editor.vim.toggle", "view.toggleVimMode")]
    public void Every_Shortcut_Id_Maps_Onto_Its_Command(string shortcutId, string commandId)
    {
        var doc = ParseShortcut($$$"""{"id": "{{{shortcutId}}}"}""");

        Apply(doc);

        Assert.Equal(commandId, (string?)Shortcut(doc, 0)["id"]);
    }

    [Fact]
    public void An_Id_With_No_Mapping_Is_Left_Alone()
    {
        var doc = ParseShortcut("""{"id": "file.save"}""");

        Apply(doc);

        Assert.Equal("file.save", (string?)Shortcut(doc, 0)["id"]);
    }

    [Theory]
    [InlineData("KeyA", "A")]
    [InlineData("KeyZ", "Z")]
    [InlineData("Digit0", "0")]
    [InlineData("Digit9", "9")]
    [InlineData("F1", "F1")]
    [InlineData("F12", "F12")]
    [InlineData("Tab", "Tab")]
    [InlineData("Enter", "Enter")]
    [InlineData("Escape", "Escape")]
    [InlineData("Space", "Space")]
    [InlineData("ArrowUp", "ArrowUp")]
    [InlineData("Equal", "=")]
    [InlineData("Minus", "-")]
    [InlineData("Comma", ",")]
    [InlineData("Semicolon", ";")]
    [InlineData("Backquote", "`")]
    [InlineData("BracketLeft", "[")]
    [InlineData("Backslash", "\\")]
    [InlineData("Quote", "'")]
    [InlineData("Numpad5", "5")]
    [InlineData("NumpadAdd", "+")]
    public void Key_Codes_Map_Onto_The_Key_They_Produce(string keyCode, string expected)
    {
        var doc = ParseShortcut($$$"""{"id": "a", "key": "{{{keyCode.Replace("\\", "\\\\")}}}"}""");

        Apply(doc);

        Assert.Equal(expected, (string?)Shortcut(doc, 0)["key"]);
    }

    [Theory]
    [InlineData("ControlLeft")]
    [InlineData("ShiftRight")]
    [InlineData("MetaLeft")]
    [InlineData("CapsLock")]
    [InlineData("Unknown")]
    public void Key_Codes_That_Cannot_End_A_Combination_Lose_Their_Key(string keyCode)
    {
        var doc = ParseShortcut($$$"""{"id": "a", "ctrl": true, "key": "{{{keyCode}}}"}""");

        Apply(doc);

        var shortcut = Shortcut(doc, 0);
        Assert.Null((string?)shortcut["key"]);
        Assert.True((bool)shortcut["primary"]!);
    }

    [Fact]
    public void A_Shortcut_With_No_Key_Is_Left_Alone()
    {
        var doc = ParseShortcut("""{"id": "a", "ctrl": true}""");

        Apply(doc);

        var shortcut = Shortcut(doc, 0);
        Assert.False(shortcut.ContainsKey("key"));
        Assert.True((bool)shortcut["primary"]!);
    }

    [Fact]
    public void A_Shortcut_Already_Carrying_The_V1_Shape_Passes_Through_Unharmed()
    {
        var doc = ParseShortcut("""{"id": "file.save", "primary": true, "key": "S"}""");

        Apply(doc);

        var shortcut = Shortcut(doc, 0);
        Assert.Equal("file.save", (string?)shortcut["id"]);
        Assert.True((bool)shortcut["primary"]!);
        Assert.Equal("S", (string?)shortcut["key"]);
    }

    [Theory]
    [InlineData("""{"keyboardShortcuts": null}""")]
    [InlineData("""{"keyboardShortcuts": {"shortcuts": null}}""")]
    [InlineData("""{"keyboardShortcuts": {"shortcuts": ["not an object"]}}""")]
    public void Absent_Or_Unexpected_Keyboard_Sections_Are_Tolerated(string json)
    {
        var doc = Parse(json);

        Apply(doc);

        Assert.Equal(1, (int)doc["version"]!);
    }

    [Fact]
    public void Every_Combination_Resolves_Identically_After_Migrating()
    {
        var pipeline = new JsonMigrationPipeline([new SettingsFileV0ToV1MigrationStep()]);

        var settings = pipeline.MigrateToLatest<Settings>(
            """
            {
              "keyboardShortcuts": {
                "shortcuts": [
                  { "id": "shortcut.documents.save", "ctrl": true, "key": "KeyS" },
                  { "id": "shortcut.window.zoomIn", "ctrl": true, "key": "Equal" },
                  { "id": "shortcut.namespaces.open", "alt": true, "key": "KeyN" },
                  { "id": "shortcut.commandpalette.open", "key": "F1" }
                ]
              }
            }
            """,
            JsonSerializer.DefaultOptions);

        Assert.Equal(1, settings.Version);

        var shortcuts = settings.KeyboardShortcuts.Shortcuts;
        Assert.Equal(4, shortcuts.Count);

        Assert.Equivalent(new { Primary = true, Alt = false, Shift = false, Meta = false, Key = "S" },
            Combination(shortcuts, "file.save"));
        Assert.Equivalent(new { Primary = true, Alt = false, Shift = false, Meta = false, Key = "=" },
            Combination(shortcuts, "view.zoomIn"));
        Assert.Equivalent(new { Primary = false, Alt = true, Shift = false, Meta = false, Key = "N" },
            Combination(shortcuts, "view.namespaces"));
        Assert.Equivalent(new { Primary = false, Alt = false, Shift = false, Meta = false, Key = "F1" },
            Combination(shortcuts, "view.commandPalette"));
    }

    [Fact]
    public void Retired_Keyboard_Keys_Are_Not_Written_Back()
    {
        var pipeline = new JsonMigrationPipeline([new SettingsFileV0ToV1MigrationStep()]);

        var settings = pipeline.MigrateToLatest<Settings>(
            """{"keyboardShortcuts": {"shortcuts": [{"id": "shortcut.documents.save", "ctrl": true, "key": "KeyS"}]}}""",
            JsonSerializer.DefaultOptions);

        var json = JsonSerializer.Serialize(settings);

        Assert.DoesNotContain("\"ctrl\"", json);
        Assert.DoesNotContain("shortcut.documents.save", json);
        Assert.Contains("\"id\":\"file.save\"", json);
        Assert.Contains("\"primary\":true", json);
        Assert.Contains("\"key\":\"S\"", json);
    }

    private static object Combination(IEnumerable<KeyboardShortcutConfiguration> shortcuts, string id)
    {
        var shortcut = shortcuts.Single(s => s.Id == id);
        return new
        {
            shortcut.Primary,
            shortcut.Alt,
            shortcut.Shift,
            shortcut.Meta,
            shortcut.Key
        };
    }

    private static void Apply(JsonObject doc) => new SettingsFileV0ToV1MigrationStep().Apply(doc);

    private static JsonObject Parse(string json) => (JsonObject)JsonNode.Parse(json)!;

    private static JsonObject ParseAppearance(string appearanceJson) =>
        Parse($$"""{"appearance": {{appearanceJson}}}""");

    private static JsonObject ParseShortcut(string shortcutJson) =>
        Parse($$$"""{"keyboardShortcuts": {"shortcuts": [{{{shortcutJson}}}]}}""");

    private static JsonObject ParseMonacoOptions(string monacoOptionsJson) =>
        Parse($$$"""{"editor": {"monacoOptions": {{{monacoOptionsJson}}}}}""");

    private static JsonObject Appearance(JsonObject doc) => (JsonObject)doc["appearance"]!;

    private static JsonObject MonacoOptions(JsonObject doc) => (JsonObject)doc["editor"]!["monacoOptions"]!;

    private static JsonObject Shortcut(JsonObject doc, int index) =>
        (JsonObject)((JsonArray)doc["keyboardShortcuts"]!["shortcuts"]!)[index]!;

    private static string Json(bool value) => value ? "true" : "false";
}
