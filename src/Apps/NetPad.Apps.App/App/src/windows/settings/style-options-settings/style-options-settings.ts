import {bindable, ILogger, IObserverLocator} from "aurelia";
import {IDisposable} from "@common";
import {EditorThemeSettings, MonacoEditorUtil, Settings} from "@application";
import * as monaco from "monaco-editor";
import {editorThemeSettings} from "../editor-theme-settings";

export class StyleOptionsSettings {
    @bindable public settings: Settings;
    public currentSettings: Readonly<Settings>;
    private editor: monaco.editor.IStandaloneCodeEditor;
    private themeSettings: EditorThemeSettings;
    private editorSettingsWatch?: IDisposable;
    private logger: ILogger;

    constructor(
        currentSettings: Settings,
        @IObserverLocator private readonly observerLocator: IObserverLocator,
        @ILogger logger: ILogger) {
        this.currentSettings = currentSettings;
        this.logger = logger.scopeTo(nameof(StyleOptionsSettings));
    }

    public attached() {
        const id = "style-editor";
        const el = document.getElementById(id);
        if (!el) {
            this.logger.error(`Could not find element with ID '${id}'. Cannot initialize editor.`);
            return;
        }

        this.editor = monaco.editor.create(el, {
            value: this.currentSettings.styles.customCss || "/*  Write your custom CSS styles here */\n" +
                ".dump-container {\n" +
                "\tfont-size: 1rem;\n" +
                "}",
            language: 'css',
            automaticLayout: true,
        });

        this.themeSettings = editorThemeSettings(this.currentSettings, this.settings);
        this.updateEditorOptions();
        this.editorSettingsWatch = MonacoEditorUtil.onEditorSettingsChanged(
            this.observerLocator,
            this.themeSettings,
            () => void this.updateEditorOptions());

        this.editor.onDidChangeModelContent(ev => {
            this.settings.styles.customCss = this.editor.getValue();
        });
    }

    public detaching() {
        this.editorSettingsWatch?.dispose();
        this.editor.dispose();
    }

    private async updateEditorOptions() {
        await MonacoEditorUtil.updateOptions(this.editor, this.themeSettings);
    }
}
