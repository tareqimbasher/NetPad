import {bindable, ILogger, PLATFORM, watch} from "aurelia";
import * as monaco from "monaco-editor";
import {IScriptService, ISession, ScriptEnvironment, Settings} from "@domain";
import {ViewModelBase} from "@application/view-model-base";

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
        this.monacoEditor.dispose();
        super.detaching();
    }

    public focus() {
        setTimeout(() => this.monacoEditor?.focus(), 50);
    }

    private initializeEditor() {
        this.monacoEditor = monaco.editor.create(this.element as HTMLElement, {
            value: this.environment.script.code,
            language: 'csharp'
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
    @watch<Editor>(vm => vm.settings.editorOptions)
    private updateEditorSettings() {
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

        Object.assign(options, this.settings.editorOptions || {})
        this.monacoEditor.updateOptions(options);
    }
}
