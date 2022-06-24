import {bindable, ILogger, watch} from "aurelia";
import {observable} from "@aurelia/runtime";
import * as monaco from "monaco-editor";
import {Settings} from "@domain";

export class EditorOptionsSettings {
    @bindable public settings: Settings;
    @observable public useCustomEditorBackgroundColor: boolean;
    public currentSettings: Readonly<Settings>;

    private editor: monaco.editor.IStandaloneCodeEditor;
    private logger: ILogger;

    constructor(currentSettings: Settings, @ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(EditorOptionsSettings));
        this.currentSettings = currentSettings;
    }

    public binding() {
        this.useCustomEditorBackgroundColor = !!this.settings.editorBackgroundColor;
    }

    public attached() {
        const el = document.getElementById("options-editor");
        this.editor = monaco.editor.create(el, {
            value: JSON.stringify(this.settings.editorOptions.monacoOptions, null, 4),
            language: 'json',
            mouseWheelZoom: true,
            automaticLayout: true
        });

        this.updateEditorOptions(this.settings.editorOptions.monacoOptions);

        this.editor.onDidChangeModelContent(ev => {
            const json = this.editor.getValue();
            let options;

            try {
                options = JSON.parse(json);
            } catch (ex) {
                this.logger.error("Error parsing editor options", ex);
                return;
            }

            this.settings.editorOptions.monacoOptions = options;
        });
    }

    public detaching() {
        this.editor.dispose();
    }

    public useCustomEditorBackgroundColorChanged(newValue: boolean) {
        if (!newValue) this.settings.editorBackgroundColor = null;
    }

    @watch<EditorOptionsSettings>(vm => vm.settings.editorBackgroundColor)
    @watch<EditorOptionsSettings>(vm => vm.settings.editorOptions.monacoOptions)
    private updateEditorOptions(editorOptions?: any) {
        let theme = this.settings.theme === "Light" ? "vs" : "vs-dark";

        if (this.settings.editorBackgroundColor) {
            monaco.editor.defineTheme("custom-theme", {
                base: <any>theme,
                inherit: true,
                rules: [],
                colors: {
                    "editor.background": this.settings.editorBackgroundColor,
                },
            });
            theme = "custom-theme";
        }

        const options = {
            theme: theme
        };

        Object.assign(options, this.settings.editorOptions.monacoOptions || {})
        this.editor.updateOptions(options);

        if (editorOptions)
            this.editor.updateOptions(editorOptions);
    }
}
