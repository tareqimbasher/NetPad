import {bindable, ILogger, PLATFORM} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import * as monaco from "monaco-editor";
import {IScriptService, ISession, ScriptEnvironment, Settings} from "@domain";
import {ViewModelBase, EditorUtil, EditorSetup} from "@application";

export class Editor extends ViewModelBase {
    @bindable public environment: ScriptEnvironment;
    @bindable public text: string;
    @bindable public viewModel: () => Editor;
    private monacoEditor?: monaco.editor.IStandaloneCodeEditor;

    constructor(
        readonly element: Element,
        readonly settings: Settings,
        @ISession readonly session: ISession,
        @IScriptService readonly scriptService: IScriptService,
        @ILogger logger: ILogger) {
        super(logger);
    }

    public async attached(): Promise<void> {
        this.viewModel = () => this;

        PLATFORM.taskQueue.queueTask(() => {
            this.initializeEditor();
        }, { delay: 100 });
    }

    public override detaching() {
        this.monacoEditor.getModel().dispose();
        this.monacoEditor.dispose();
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

        this.monacoEditor.onDidChangeModelContent(ev => this.text = this.monacoEditor.getValue());

        const ob = new ResizeObserver(entries => this.updateEditorLayout());
        ob.observe(this.element);
        this.disposables.push(() => ob.disconnect());
    }

    @watch<Editor>(vm => vm.session.active)
    private activeScriptEnvironmentChanged() {
        PLATFORM.taskQueue.queueTask(() => {
            if (this.environment === this.session.active)
                this.updateEditorLayout();
        }, {delay: 100});
    }

    private updateEditorLayout() {
        this.monacoEditor.layout();
    }

    @watch<Editor>(vm => vm.settings.theme)
    @watch<Editor>(vm => vm.settings.editorBackgroundColor)
    @watch<Editor>(vm => vm.settings.editorOptions.monacoOptions)
    private updateEditorSettings() {
        let theme = this.settings.theme === "Light" ? "netpad-light-theme" : "netpad-dark-theme";

        if (this.settings.editorBackgroundColor) {
            const base: monaco.editor.BuiltinTheme = this.settings.theme === "Light" ? "vs" : "vs-dark";

            EditorSetup.defineTheme("custom-theme", {
                base: base,
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
        this.monacoEditor.updateOptions(options);
    }
}
