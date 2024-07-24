import {bindable, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {observable} from "@aurelia/runtime";
import * as monaco from "monaco-editor";
import {MonacoEditorUtil, Settings} from "@application";
import {MonacoThemeManager} from "@application/editor/monaco/monaco-theme-manager";

export class EditorOptionsSettings {
    @bindable public settings: Settings;
    @observable public theme?: string;

    private editor: monaco.editor.IStandaloneCodeEditor;
    private logger: ILogger;

    constructor(@ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(EditorOptionsSettings));
    }

    public async attached() {
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
            mouseWheelZoom: true,
            automaticLayout: true
        });

        this.updateEditorOptions();

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
        this.editor.dispose();
    }

    public getThemes() {
        return [...MonacoThemeManager.getThemes()]
            .sort((a, b) => a.name.localeCompare(b.name));
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

    @watch<EditorOptionsSettings>(vm => vm.settings.editor.monacoOptions)
    private async updateEditorOptions(monacoOptions?: monaco.editor.IEditorOptions & monaco.editor.IGlobalEditorOptions) {
        await MonacoEditorUtil.updateOptions(this.editor, this.settings);
    }
}
