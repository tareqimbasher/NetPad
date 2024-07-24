import {bindable, ILogger} from "aurelia";
import {MonacoEditorUtil, Settings} from "@application";
import * as monaco from "monaco-editor";
import {watch} from "@aurelia/runtime-html";

export class StyleOptionsSettings {
    @bindable public settings: Settings;
    public currentSettings: Readonly<Settings>;
    private editor: monaco.editor.IStandaloneCodeEditor;
    private logger: ILogger;

    constructor(currentSettings: Settings, @ILogger logger: ILogger) {
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
            mouseWheelZoom: true,
            automaticLayout: true,
        });

        this.updateEditorOptions();

        this.editor.onDidChangeModelContent(ev => {
            this.settings.styles.customCss = this.editor.getValue();
        });
    }

    public detaching() {
        this.editor.dispose();
    }

    @watch<StyleOptionsSettings>(vm => vm.currentSettings.editor.monacoOptions)
    private async updateEditorOptions() {
        await MonacoEditorUtil.updateOptions(this.editor, this.settings);
    }
}
