import {EditorThemeSettings, Settings} from "@application";

/**
 * What an editor in the settings window takes its theme from: appearance and custom CSS from the
 * saved settings, which is what the window itself shows, and editor options from the copy being
 * edited, which is what the editor previews.
 */
export function editorThemeSettings(saved: Readonly<Settings>, editable: Settings): EditorThemeSettings {
    return new SettingsWindowEditorTheme(saved, editable);
}

class SettingsWindowEditorTheme implements EditorThemeSettings {
    constructor(private readonly saved: Readonly<Settings>, private readonly editable: Settings) {
    }

    public get appearance() {
        return this.saved.appearance;
    }

    public get styles() {
        return this.saved.styles;
    }

    public get editor() {
        return this.editable.editor;
    }
}
