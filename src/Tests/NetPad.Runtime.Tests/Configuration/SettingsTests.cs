using NetPad.Configuration;

namespace NetPad.Runtime.Tests.Configuration;

public class SettingsTests
{
    [Fact]
    public void Default_Values()
    {
        var settings = new Settings("foo", "bar");

        Assert.Equal("foo", settings.ScriptsDirectoryPath);
        Assert.Equal("bar", settings.PackageCacheDirectoryPath);
        Assert.True(settings.AutoCheckUpdates);
    }
}

public class SettingsTests2
{
    [Fact]
    public void Constructor_Sets_Default_Values()
    {
        var settings = new Settings();

        Assert.NotNull(settings.Version);
        Assert.Equal(new Version("1.0"), settings.Version);

        Assert.True(settings.AutoCheckUpdates.HasValue && settings.AutoCheckUpdates.Value);

        Assert.Equal(AppDataProvider.Defaults.ScriptsDirectoryPath.Path, settings.ScriptsDirectoryPath);
        Assert.Equal(AppDataProvider.Defaults.AutoSaveScriptsDirectoryPath.Path, settings.AutoSaveScriptsDirectoryPath);
        Assert.Equal(AppDataProvider.Defaults.PackageCacheDirectoryPath.Path, settings.PackageCacheDirectoryPath);

        Assert.NotNull(settings.Appearance);
        Assert.NotNull(settings.Editor);
        Assert.NotNull(settings.Results);
        Assert.NotNull(settings.Styles);
        Assert.NotNull(settings.KeyboardShortcuts);
        Assert.NotNull(settings.OmniSharp);
    }

    [Fact]
    public void Constructor_With_Paths_Sets_Properties()
    {
        const string scripts = "/path/scripts";
        const string cache = "/path/cache";

        var settings = new Settings(scripts, cache);

        Assert.Equal(scripts, settings.ScriptsDirectoryPath);
        Assert.Equal(cache, settings.PackageCacheDirectoryPath);
    }

    [Fact]
    public void Set_Scripts_Directory_Path_Null_Throws()
    {
        var settings = new Settings();
        Assert.Throws<ArgumentNullException>(() => settings.SetScriptsDirectoryPath(null!));
    }

    [Fact]
    public void Set_Package_Cache_Directory_Path_Null_Throws()
    {
        var settings = new Settings();
        Assert.Throws<ArgumentNullException>(() => settings.SetPackageCacheDirectoryPath(null!));
    }

    [Fact]
    public void Set_Appearance_Options_Copies_Values_And_Is_Fluent()
    {
        var settings = new Settings();

        var appearance = new AppearanceOptions()
            .SetThemeFamily("gunmetal")
            .SetMode(ThemeMode.Light)
            .SetShowScriptRunStatusIndicatorInTab(false)
            .SetScriptRunStatusIndicatorInExplorer(StatusIndicatorVisibility.WhileRunning)
            .SetTitlebarOptions(new TitlebarOptions()
                .SetType(TitlebarType.Integrated)
                .SetWindowControlsPosition(WindowControlsPosition.Left)
                .SetMainWindowVisibility(MainMenuVisibility.AutoHidden)
            );

        var returned = settings.SetAppearanceOptions(appearance);

        Assert.Same(settings, returned);
        Assert.Equal("gunmetal", settings.Appearance.ThemeFamily);
        Assert.Equal(ThemeMode.Light, settings.Appearance.Mode);
        Assert.False(settings.Appearance.ShowScriptRunStatusIndicatorInTab);
        Assert.Equal(StatusIndicatorVisibility.WhileRunning, settings.Appearance.ScriptRunStatusIndicatorInExplorer);
        Assert.Equal(TitlebarType.Integrated, settings.Appearance.Titlebar.Type);
        Assert.Equal(WindowControlsPosition.Left, settings.Appearance.Titlebar.WindowControlsPosition);
        Assert.Equal(MainMenuVisibility.AutoHidden, settings.Appearance.Titlebar.MainMenuVisibility);
    }

    [Fact]
    public void Set_Appearance_Options_Null_Throws()
    {
        var settings = new Settings();
        Assert.Throws<ArgumentNullException>(() => settings.SetAppearanceOptions(null!));
    }

    [Fact]
    public void Set_Editor_Options_Copies_Values_And_Is_Fluent()
    {
        var settings = new Settings();

        var editor = new EditorOptions()
            .SetMonacoOptions(new { fontSize = 18, lineNumbers = "on" })
            .SetVimOptions(new VimOptions().SetEnabled(true));

        var returned = settings.SetEditorOptions(editor);

        Assert.Same(settings, returned);
        Assert.NotNull(settings.Editor.MonacoOptions);
        Assert.True(settings.Editor.Vim.Enabled);
    }

    [Fact]
    public void Set_Results_Options_Copies_Values_And_Is_Fluent()
    {
        var settings = new Settings();

        var results = new ResultsOptions()
            .SetOpenOnRun(false)
            .SetTextWrap(true)
            .SetMaxSerializationDepth(128)
            .SetMaxCollectionSerializeLengthDepth(2000);

        var returned = settings.SetResultsOptions(results);

        Assert.Same(settings, returned);
        Assert.False(settings.Results.OpenOnRun);
        Assert.True(settings.Results.TextWrap);
        Assert.Equal((uint)128, settings.Results.MaxSerializationDepth);
        Assert.Equal((uint)2000, settings.Results.MaxCollectionSerializeLength);
    }

    [Fact]
    public void Set_Style_Options_Copies_Values_And_Is_Fluent()
    {
        var settings = new Settings();

        var style = new StyleOptions()
            .SetEnabled(true)
            .SetCustomCss("body { margin: 0; }");

        var returned = settings.SetStyleOptions(style);

        Assert.Same(settings, returned);
        Assert.True(settings.Styles.Enabled);
        Assert.Equal("body { margin: 0; }", settings.Styles.CustomCss);
    }

    [Fact]
    public void Set_Style_Options_Null_Throws()
    {
        var settings = new Settings();
        Assert.Throws<ArgumentNullException>(() => settings.SetStyleOptions(null!));
    }

    [Fact]
    public void Set_Keyboard_Shortcut_Options_Copies_Values_And_Is_Fluent()
    {
        var settings = new Settings();

        var shortcuts = new List<KeyboardShortcutConfiguration>
        {
            new KeyboardShortcutConfiguration("open-command-palette") { Ctrl = true, Shift = true, Key = KeyCode.KeyP },
            new KeyboardShortcutConfiguration("run-script") { Ctrl = true, Key = KeyCode.Enter }
        };

        var keyboard = new KeyboardShortcutOptions().SetShortcuts(shortcuts);

        var returned = settings.SetKeyboardShortcutOptions(keyboard);

        Assert.Same(settings, returned);
        Assert.Equal(2, settings.KeyboardShortcuts.Shortcuts.Count);
        Assert.Contains(settings.KeyboardShortcuts.Shortcuts, s => s.Id == "open-command-palette");
        Assert.Contains(settings.KeyboardShortcuts.Shortcuts, s => s.Id == "run-script");
    }

    [Fact]
    public void Set_Keyboard_Shortcut_Options_With_Duplicate_Ids_Throws()
    {
        var settings = new Settings();

        var shortcuts = new List<KeyboardShortcutConfiguration>
        {
            new KeyboardShortcutConfiguration("dup"),
            new KeyboardShortcutConfiguration("dup")
        };

        var keyboard = new KeyboardShortcutOptions();

        Assert.Throws<ArgumentException>(() => keyboard.SetShortcuts(shortcuts));
    }

    [Fact]
    public void Set_OmniSharp_Options_Is_Fluent()
    {
        var settings = new Settings();
        var returned = settings.SetOmniSharpOptions(new OmniSharpOptions());
        Assert.Same(settings, returned);
        Assert.NotNull(settings.OmniSharp);
    }

    [Fact]
    public void Retired_Appearance_Properties_In_Saved_Settings_Are_Ignored()
    {
        // Settings files written before the icon-theme setting was removed still carry it.
        const string json = """
                            {
                              "appearance": {
                                "iconTheme": "Colorful",
                                "showScriptRunStatusIndicatorInTab": false
                              }
                            }
                            """;

        var settings = Deserialize(json);

        Assert.False(settings.Appearance.ShowScriptRunStatusIndicatorInTab);
    }

    [Theory]
    [InlineData("Dark", ThemeMode.Dark)]
    [InlineData("Light", ThemeMode.Light)]
    public void Retired_Theme_Maps_Onto_Mode(string retiredTheme, ThemeMode expected)
    {
        var settings = Deserialize($$"""
                                     {
                                       "appearance": { "theme": "{{retiredTheme}}" }
                                     }
                                     """);

        Assert.Equal(expected, settings.Appearance.Mode);
        Assert.Equal(AppearanceOptions.DefaultThemeFamily, settings.Appearance.ThemeFamily);
    }

    [Fact]
    public void Retired_Theme_Is_Not_Written_Back()
    {
        var settings = Deserialize("""{"appearance": {"theme": "Light"}}""");

        var json = NetPad.Common.JsonSerializer.Serialize(settings);

        Assert.DoesNotContain("\"theme\"", json);
        Assert.Contains("\"mode\":\"Light\"", json);
    }

    [Fact]
    public void Fresh_Settings_Default_To_System_Mode_And_The_Default_Family()
    {
        var settings = new Settings();

        Assert.Equal(ThemeMode.System, settings.Appearance.Mode);
        Assert.Equal(AppearanceOptions.DefaultThemeFamily, settings.Appearance.ThemeFamily);
    }

    [Fact]
    public void Unknown_Theme_Values_Fall_Back_To_Defaults()
    {
        var settings = Deserialize("""{"appearance": {"mode": "Twilight", "themeFamily": "  "}}""");

        Assert.Equal(ThemeMode.System, settings.Appearance.Mode);
        Assert.Equal(AppearanceOptions.DefaultThemeFamily, settings.Appearance.ThemeFamily);
    }

    [Theory]
    [InlineData(true, true, StatusIndicatorVisibility.Always)]
    [InlineData(true, false, StatusIndicatorVisibility.Always)]
    [InlineData(false, true, StatusIndicatorVisibility.WhileRunning)]
    [InlineData(false, false, StatusIndicatorVisibility.Off)]
    public void Retired_Explorer_Indicator_Booleans_Map_Onto_The_Tri_State(
        bool showStatusIndicator,
        bool showRunningIndicator,
        StatusIndicatorVisibility expected)
    {
        var json = $$"""
                     {
                       "appearance": {
                         "showScriptRunStatusIndicatorInScriptsList": {{(showStatusIndicator ? "true" : "false")}},
                         "showScriptRunningIndicatorInScriptsList": {{(showRunningIndicator ? "true" : "false")}}
                       }
                     }
                     """;

        var settings = Deserialize(json);

        Assert.Equal(expected, settings.Appearance.ScriptRunStatusIndicatorInExplorer);
    }

    [Fact]
    public void Retired_Explorer_Indicator_Booleans_Are_Not_Written_Back()
    {
        var settings = Deserialize("""{"appearance": {"showScriptRunningIndicatorInScriptsList": true}}""");

        var json = NetPad.Common.JsonSerializer.Serialize(settings);

        Assert.DoesNotContain("IndicatorInScriptsList", json);
        Assert.Contains("\"scriptRunStatusIndicatorInExplorer\":\"WhileRunning\"", json);
    }

    [Fact]
    public void Absent_Explorer_Indicator_Settings_Default_To_Off()
    {
        var settings = Deserialize("""{"appearance": {}}""");

        Assert.Equal(StatusIndicatorVisibility.Off, settings.Appearance.ScriptRunStatusIndicatorInExplorer);
    }

    [Fact]
    public void Retired_Results_Font_In_Saved_Settings_Is_Ignored_And_Not_Written_Back()
    {
        const string json = """
                            {
                              "results": {
                                "font": "monospace",
                                "textWrap": true
                              }
                            }
                            """;

        var settings = Deserialize(json);

        Assert.True(settings.Results.TextWrap);

        var serialized = NetPad.Common.JsonSerializer.Serialize(settings);

        Assert.DoesNotContain("\"font\"", serialized);
    }

    [Fact]
    public void Unknown_Explorer_Indicator_Value_Falls_Back_To_Off()
    {
        var settings = Deserialize("""{"appearance": {"scriptRunStatusIndicatorInExplorer": "Sometimes"}}""");

        Assert.Equal(StatusIndicatorVisibility.Off, settings.Appearance.ScriptRunStatusIndicatorInExplorer);
    }

    private static Settings Deserialize(string json)
    {
        var settings = NetPad.Common.JsonSerializer.Deserialize<Settings>(json);

        Assert.NotNull(settings);

        // What FileSystemSettingsRepository does after every load, and where migration runs.
        settings.DefaultMissingValues();

        return settings;
    }

    [Fact]
    public void Default_Missing_Values_Does_Not_Overwrite_Non_Nulls()
    {
        var settings = new Settings()
            .SetAutoCheckUpdates(false)
            .SetScriptsDirectoryPath("C:\\custom\\scripts")
            .SetPackageCacheDirectoryPath("C:\\custom\\cache");

        var prevVersion = settings.Version;

        settings.DefaultMissingValues();

        Assert.Equal(prevVersion, settings.Version);
        Assert.False(settings.AutoCheckUpdates!.Value);
        Assert.Equal("C:\\custom\\scripts", settings.ScriptsDirectoryPath);
        Assert.Equal("C:\\custom\\cache", settings.PackageCacheDirectoryPath);
    }
}
