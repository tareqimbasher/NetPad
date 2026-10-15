import {bindable, ILogger, IObserverLocator, resolve} from "aurelia";
import {observable} from "@aurelia/runtime";
import * as monaco from "monaco-editor";
import {IDisposable} from "@common";
import {CommandIds, EditorThemeSettings, MonacoEditorUtil, resolveKeybindings, Settings} from "@application";
import {MonacoThemeManager} from "@application/editor/monaco/monaco-theme-manager";
import {editorThemeSettings} from "../editor-theme-settings";

export class EditorOptionsSettings {
    @bindable public settings: Settings;
    @observable public theme?: string;
    public vimModeKeyCombo?: string;

    private editor: monaco.editor.IStandaloneCodeEditor;
    private themeSettings: EditorThemeSettings;
    private editorSettingsWatch?: IDisposable;
    private readonly savedSettings: Readonly<Settings> = resolve(Settings);
    private readonly observerLocator = resolve(IObserverLocator);
    private logger: ILogger = resolve(ILogger).scopeTo(nameof(EditorOptionsSettings));

    public async attached() {
        const keyCombo = resolveKeybindings(this.settings)
            .find(k => k.commandId === CommandIds.toggleVimMode)?.keyCombo;
        this.vimModeKeyCombo = keyCombo?.isBound ? keyCombo.asString() : undefined;

        // Initialize monaco editor
        const id = "options-editor";
        const el = document.getElementById(id);
        if (!el) {
            this.logger.error(`Could not find element with ID '${id}'. Cannot initialize editor.`);
            return;
        }

        const monacoOptions = JSON.parse(JSON.stringify(this.settings.editor.monacoOptions ?? {}))

        if (!monacoOptions.themeCustomizations) {
            monacoOptions.themeCustomizations =
                {
                    colors: {},
                    rules: []
                }
        }

        this.editor = monaco.editor.create(el, {
            value: JSON.stringify(monacoOptions, null, 4),
            language: 'json',
            automaticLayout: true
        });

        this.themeSettings = editorThemeSettings(this.savedSettings, this.settings);
        this.updateEditorOptions();
        this.editorSettingsWatch = MonacoEditorUtil.onEditorSettingsChanged(
            this.observerLocator,
            this.themeSettings,
            () => void this.updateEditorOptions());

        this.editor.onDidChangeModelContent(ev => {
            const json = this.editor.getValue();
            let options;

            try {
                options = JSON.parse(json);
            } catch (ex) {
                this.logger.error("Error parsing editor options", ex);
                return;
            }

            this.settings.editor.monacoOptions = options;
        });

        this.theme = this.settings.editor.monacoOptions?.theme;
    }

    public detaching() {
        this.editorSettingsWatch?.dispose();
        this.editor.dispose();
    }

    public get netPadThemes() {
        return MonacoThemeManager.getNetPadThemes();
    }

    public get libraryThemes() {
        return MonacoThemeManager.getLibraryThemes();
    }

    public themeChanged(newValue: string) {
        const val = this.editor.getValue();
        if (!val) {
            return;
        }

        try {
            let o = JSON.parse(val);

            if (!o) {
                return;
            }

            if (!newValue) {
                delete o.theme;
            } else {
                if (Object.hasOwn(o, "theme")) {
                    o.theme = newValue;
                } else {
                    const tmp = {theme: newValue};
                    Object.assign(tmp, o);
                    o = tmp;
                }
            }

            this.editor.setValue(JSON.stringify(o, null, 4));
        } catch (e) {
            this.logger.error("Error updating editor with new theme");
        }
    }

    private async updateEditorOptions() {
        await MonacoEditorUtil.updateOptions(this.editor, this.themeSettings);
    }
}
