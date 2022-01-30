import {bindable, PLATFORM, watch} from "aurelia";
import * as monaco from "monaco-editor";
import {IScriptService, ISession, ScriptEnvironment, Settings} from "@domain";
import {Util} from "@common";
import {TestCompletionItemProvider} from "./completion-item-providers/test-completion-item-provider";

export class Editor {
    @bindable public environment: ScriptEnvironment;
    private monacoEditor: monaco.editor.IStandaloneCodeEditor;

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

    private initializeEditor() {
        this.monacoEditor = monaco.editor.create(this.element as HTMLElement, {
            value: this.environment.script.code,
            language: 'csharp'
        });
        this.updateEditorTheme();

        // TODO should be called once per app lifetime. Here for testing.
        new TestCompletionItemProvider().register();

        const f = Util.debounce(this, async (ev) => {
            await this.scriptService.updateCode(this.environment.script.id, this.monacoEditor.getValue());
        }, 500, true);

        this.monacoEditor.onDidChangeModelContent(ev => f(ev));

        window.addEventListener("resize", () => this.monacoEditor.layout());
        // const ob = new ResizeObserver(entries => {
        //     console.log(entries);
        //     this.editor.layout({
        //         width: document.scriptSelector(".window").clientWidth - document.scriptSelector("sidebar").clientWidth,
        //         height: document.scriptSelector(".text-editor").clientHeight
        //     });
        // });
        // ob.observe(document.scriptSelector("statusbar"));
    }

    @watch<Editor>(vm => vm.settings.theme)
    private updateEditorTheme() {
        monaco.editor.setTheme(this.settings.theme === "Light" ? "vs" : "vs-dark");
    }
}
