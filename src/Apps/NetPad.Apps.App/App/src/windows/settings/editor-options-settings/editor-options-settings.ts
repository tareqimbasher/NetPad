import {bindable, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {observable} from "@aurelia/runtime";
import * as monaco from "monaco-editor";
import {Settings} from "@domain";

export class EditorOptionsSettings {
    @bindable public settings: Settings;
    @observable public useCustomEditorBackgroundColor: boolean;

    private editor: monaco.editor.IStandaloneCodeEditor;
    private logger: ILogger;

    constructor(@ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(EditorOptionsSettings));
    }

    public binding() {
        this.useCustomEditorBackgroundColor = !!this.settings.editor.backgroundColor;
    }

    public attached() {
        const id = "options-editor";
        const el = document.getElementById(id);
        if (!el) {
            this.logger.error(`Could not find element with ID '${id}'. Cannot initialize editor.`);
            return;
        }

        this.editor = monaco.editor.create(el, {
            value: JSON.stringify(this.settings.editor.monacoOptions, null, 4),
            language: 'json',
            mouseWheelZoom: true,
            automaticLayout: true
        });

        this.updateEditorOptions(this.settings.editor.monacoOptions);

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
    }

    public detaching() {
        this.editor.dispose();
    }

    public useCustomEditorBackgroundColorChanged(newValue: boolean) {
        if (!newValue) this.settings.editor.backgroundColor = undefined;
    }

    @watch<EditorOptionsSettings>(vm => vm.settings.editor.backgroundColor)
    @watch<EditorOptionsSettings>(vm => vm.settings.editor.monacoOptions)
    private updateEditorOptions(editorOptions?: monaco.editor.IEditorOptions & monaco.editor.IGlobalEditorOptions) {
        let theme = this.settings.appearance.theme === "Light" ? "vs" : "vs-dark";

        if (this.settings.editor.backgroundColor) {
            monaco.editor.defineTheme("custom-theme", {
                base: theme as monaco.editor.BuiltinTheme,
                inherit: true,
                rules: [],
                colors: {
                    "editor.background": this.settings.editor.backgroundColor,
                },
            });
            theme = "custom-theme";
        }

        const options = {
            theme: theme
        };

        Object.assign(options, this.settings.editor.monacoOptions || {})
        this.editor.updateOptions(options);

        if (editorOptions)
            this.editor.updateOptions(editorOptions);
    }
}
