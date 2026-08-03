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

        Assert.Equal(1, settings.Version);

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
            .SetBackground(ThemeBackground.Neutral)
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
        Assert.Equal(ThemeBackground.Neutral, settings.Appearance.Background);
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
            new KeyboardShortcutConfiguration("open-command-palette") { Primary = true, Shift = true, Key = "P" },
            new KeyboardShortcutConfiguration("run-script") { Primary = true, Key = "Enter" }
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
    public void Fresh_Settings_Default_To_System_Mode_And_The_Default_Family()
    {
        var settings = new Settings();

        Assert.Equal(ThemeMode.System, settings.Appearance.Mode);
        Assert.Equal(AppearanceOptions.DefaultThemeFamily, settings.Appearance.ThemeFamily);
        Assert.Equal(ThemeBackground.Palette, settings.Appearance.Background);
    }

    [Fact]
    public void Unknown_Theme_Values_Fall_Back_To_Defaults()
    {
        var settings = Deserialize("""{"appearance": {"mode": "Twilight", "themeFamily": "  "}}""");

        Assert.Equal(ThemeMode.System, settings.Appearance.Mode);
        Assert.Equal(AppearanceOptions.DefaultThemeFamily, settings.Appearance.ThemeFamily);
    }

    [Fact]
    public void Absent_Or_Unknown_Background_Setting_Defaults_To_Palette()
    {
        Assert.Equal(ThemeBackground.Palette, Deserialize("""{"appearance": {}}""").Appearance.Background);
        Assert.Equal(ThemeBackground.Palette,
            Deserialize("""{"appearance": {"background": "Wallpaper"}}""").Appearance.Background);
    }

    [Fact]
    public void Absent_Explorer_Indicator_Setting_Defaults_To_Always()
    {
        var settings = Deserialize("""{"appearance": {}}""");

        Assert.Equal(StatusIndicatorVisibility.Always, settings.Appearance.ScriptRunStatusIndicatorInExplorer);
    }

    [Fact]
    public void Fresh_Settings_Show_Run_Status_In_Both_The_Tab_And_The_Explorer()
    {
        var appearance = new Settings().Appearance;

        Assert.True(appearance.ShowScriptRunStatusIndicatorInTab);
        Assert.Equal(StatusIndicatorVisibility.Always, appearance.ScriptRunStatusIndicatorInExplorer);
    }

    [Fact]
    public void Unknown_Explorer_Indicator_Value_Falls_Back_To_The_Enums_First_Member()
    {
        var settings = Deserialize("""{"appearance": {"scriptRunStatusIndicatorInExplorer": "Sometimes"}}""");

        // A value the tolerant converter cannot parse resolves to the enum's default member, which
        // is not the same as the setting's default when the key is absent altogether.
        Assert.Equal(StatusIndicatorVisibility.Off, settings.Appearance.ScriptRunStatusIndicatorInExplorer);
    }

    private static Settings Deserialize(string json)
    {
        var settings = NetPad.Common.JsonSerializer.Deserialize<Settings>(json);

        Assert.NotNull(settings);

        // What FileSystemSettingsRepository does after every load.
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

        settings.DefaultMissingValues();

        Assert.False(settings.AutoCheckUpdates!.Value);
        Assert.Equal("C:\\custom\\scripts", settings.ScriptsDirectoryPath);
        Assert.Equal("C:\\custom\\cache", settings.PackageCacheDirectoryPath);
    }
}
