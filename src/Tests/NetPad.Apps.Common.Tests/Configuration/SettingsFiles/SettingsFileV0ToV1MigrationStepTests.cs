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

    private static void Apply(JsonObject doc) => new SettingsFileV0ToV1MigrationStep().Apply(doc);

    private static JsonObject Parse(string json) => (JsonObject)JsonNode.Parse(json)!;

    private static JsonObject ParseAppearance(string appearanceJson) =>
        Parse($$"""{"appearance": {{appearanceJson}}}""");

    private static JsonObject Appearance(JsonObject doc) => (JsonObject)doc["appearance"]!;

    private static string Json(bool value) => value ? "true" : "false";
}
