import {bindable, ILogger, PLATFORM} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import * as monaco from "monaco-editor";
import {IScriptService, ISession, ScriptEnvironment, Settings} from "@domain";
import {EditorSetup, EditorUtil, ViewModelBase} from "@application";

export class Editor extends ViewModelBase {
    @bindable public environment: ScriptEnvironment;
    @bindable public text: string;
    public monacoEditor?: monaco.editor.IStandaloneCodeEditor;

    constructor(
        readonly element: Element,
        readonly settings: Settings,
        @ISession readonly session: ISession,
        @IScriptService readonly scriptService: IScriptService,
        @ILogger logger: ILogger) {
        super(logger);
    }

    public async attached(): Promise<void> {
        setTimeout(() => {
            this.initializeEditor();
        }, 100);
    }

    public override detaching() {
        this.monacoEditor?.getModel()?.dispose();
        this.monacoEditor?.dispose();
        super.detaching();
    }

    public focus() {
        setTimeout(() => this.monacoEditor?.focus(), 50);
    }

    private initializeEditor() {
        this.monacoEditor = monaco.editor.create(this.element as HTMLElement, {
            model: monaco.editor.createModel(
                this.environment.script.code,
                "csharp",
                EditorUtil.constructModelUri(this.environment.script.id)),
            "semanticHighlighting.enabled": true
        });

        this.text = this.environment.script.code;
        this.updateEditorSettings();
        this.focus();

        this.monacoEditor.onDidChangeModelContent(ev => {
            if (this.monacoEditor)
                this.text = this.monacoEditor.getValue();
        });

        const mainContent = document.getElementById("main-content");
        if (!mainContent) this.logger.error("Could not find element with ID 'main-content'");

        const ob = new ResizeObserver(entries => this.updateEditorLayout());
        if (mainContent) ob.observe(mainContent);
        ob.observe(this.element);
        this.disposables.push(() => ob.disconnect());
    }

    @watch<Editor>(vm => vm.session.active)
    private activeScriptEnvironmentChanged() {
        PLATFORM.setTimeout(() => {
            if (this.environment === this.session.active)
                this.updateEditorLayout();
        }, 100);
    }

    private updateEditorLayout() {
        this.monacoEditor?.layout();
    }

    @watch<Editor>(vm => vm.settings.appearance.theme)
    @watch<Editor>(vm => vm.settings.editor.backgroundColor)
    @watch<Editor>(vm => vm.settings.editor.monacoOptions)
    private updateEditorSettings() {
        if (!this.monacoEditor) return;

        let theme = this.settings.appearance.theme === "Light" ? "netpad-light-theme" : "netpad-dark-theme";

        if (this.settings.editor.backgroundColor) {
            const base: monaco.editor.BuiltinTheme = this.settings.appearance.theme === "Light" ? "vs" : "vs-dark";

            EditorSetup.defineTheme("custom-theme", {
                base: base,
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
        this.monacoEditor.updateOptions(options);
    }
}
