import {bindable, PLATFORM, watch} from "aurelia";
import * as monaco from "monaco-editor";
import {IScriptService, ISession, ScriptEnvironment, Settings} from "@domain";
import {TestCompletionItemProvider} from "./completion-item-providers/test-completion-item-provider";

export class Editor {
    @bindable public environment: ScriptEnvironment;
    @bindable public text: string;
    private monacoEditor: monaco.editor.IStandaloneCodeEditor;
    private disposables: (() => void)[] = [];

    constructor(
        readonly element: Element,
        readonly settings: Settings,
        @ISession readonly session: ISession,
        @IScriptService readonly scriptService: IScriptService) {
    }

    public async attached(): Promise<void> {
        PLATFORM.taskQueue.queueTask(() => {
            this.initializeEditor();
        }, { delay: 100 });
    }

    public detaching() {
        this.monacoEditor.dispose();
        this.disposables.forEach(d => d());
    }

    private initializeEditor() {
        this.monacoEditor = monaco.editor.create(this.element as HTMLElement, {
            value: this.environment.script.code,
            language: 'csharp'
        });
        this.text = this.environment.script.code;
        this.updateEditorSettings();

        this.monacoEditor.onDidChangeModelContent(ev => this.text = this.monacoEditor.getValue());

        const ob = new ResizeObserver(entries => this.updateEditorLayout());
        ob.observe(this.element);
        this.disposables.push(() => ob.disconnect());

        // TODO should be called once per app lifetime. Here for testing.
        new TestCompletionItemProvider().register();
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
