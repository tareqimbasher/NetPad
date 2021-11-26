import {bindable, PLATFORM, watch} from "aurelia";
import {IEventBus, IScriptService, ISession, Script, ScriptEnvironment, ScriptKind, ScriptOutputEmitted} from "@domain";
import * as monaco from "monaco-editor";
import {Util} from "@common";

export class ScriptEnvironmentView {
    @bindable public environment: ScriptEnvironment;
    public showResults = true;

    private disposables: (() => void)[] = [];
    private editor: monaco.editor.IStandaloneCodeEditor;
    private resultsEl: HTMLElement;

    constructor(
        @IScriptService readonly scriptService: IScriptService,
        @ISession readonly session: ISession,
        @IEventBus readonly eventBus: IEventBus) {
    }

    public get id(): string {
        return this.environment.script.id;
    }

    public get script(): Script {
        return this.environment.script;
    }

    public get kind(): ScriptKind {
        return this.script.config.kind;
    }

    public set kind(value) {
        this.scriptService.setScriptKind(this.script.id, value);
    }

    private attached() {
        this.kind = this.script.config.kind;

        const token = this.eventBus.subscribeToServer(ScriptOutputEmitted, msg => {
            if (msg.scriptId === this.environment.script.id) {
                this.appendResults(msg.output);
            }
        });

        this.disposables.push(() => token.dispose());

        PLATFORM.taskQueue.queueTask(() => {
            this.initializeEditor();
        }, {delay: 100});
    }

    public detaching() {
        this.editor.dispose();
        for (const disposable of this.disposables) {
            disposable();
        }
    }

    public async run() {
        this.setResults(null);
        this.showResults = true;
        await this.scriptService.run(this.environment.script.id);
    }

    private setResults(results: string | null) {
        this.resultsEl.innerHTML = results?.replaceAll("\n", "<br/>") ?? "";
    }

    private appendResults(results: string | null) {
        this.setResults(this.resultsEl.innerHTML + results);
    }

    private initializeEditor() {
        const el = document.querySelector(`[data-text-editor-id="${this.id}"]`) as HTMLElement;
        this.editor = monaco.editor.create(el, {
            value: this.environment.script.code,
            language: 'csharp',
            theme: "vs-dark"
        });

        const f = Util.debounce(this, async (ev) => {
            await this.scriptService.updateCode(this.environment.script.id, this.editor.getValue());
        }, 500, true);

        this.editor.onDidChangeModelContent(ev => f(ev));

        window.addEventListener("resize", () => this.editor.layout());
        // const ob = new ResizeObserver(entries => {
        //     console.log(entries);
        //     this.editor.layout({
        //         width: document.scriptSelector(".window").clientWidth - document.scriptSelector("sidebar").clientWidth,
        //         height: document.scriptSelector(".text-editor").clientHeight
        //     });
        // });
        // ob.observe(document.scriptSelector("statusbar"));
    }

    @watch<ScriptEnvironmentView>(vm => vm.session.active)
    private adjustEditorLayout() {
        PLATFORM.taskQueue.queueTask(() => {
            if (this.environment === this.session.active)
            this.editor.layout();
        }, {delay: 100});
    }
}

/**
 * Config stuff
 * UI => BE => FE
 *
 * Code
 * UI => BE
 */
