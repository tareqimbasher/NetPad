import {bindable, PLATFORM, watch} from "aurelia";
import {IScriptManager, ISession, Script} from "@domain";
import * as monaco from "monaco-editor";
import {Util} from "@common";

export class ScriptView {
    @bindable public script: Script;
    public id: string;
    public results: string = "";
    public showResults = true;

    private editor: monaco.editor.IStandaloneCodeEditor;

    constructor(
        @IScriptManager readonly scriptManager: IScriptManager,
        @ISession readonly session: ISession) {
        this.id = Util.newGuid();
    }

    private attached() {
        PLATFORM.taskQueue.queueTask(() => {
            const el = document.querySelector(`[data-text-editor-id="${this.id}"]`) as HTMLElement;
            this.editor = monaco.editor.create(el, {
                value: this.script.code,
                language: 'csharp',
                theme: "vs-dark"
            });

            const f = Util.debounce(this, async (ev) => {
                await this.scriptManager.updateCode(this.script.id, this.editor.getValue());
            }, 1000, true);

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
        }, {delay: 100});
    }

    public detaching() {
        this.editor.dispose();
    }

    public async run() {
        this.showResults = true;
        document.querySelector(`script-view[data-id="${this.id}"] .results`).innerHTML =
            (await this.scriptManager.run(this.script.id))
                .replaceAll("\n", "<br/>") ?? "";
    }

    @watch<ScriptView>(vm => vm.session.activeScript)
    private adjustEditorLayout() {
        PLATFORM.taskQueue.queueTask(() => {
            this.editor.layout();
        }, {delay: 100});
    }
}
