import {bindable, PLATFORM, watch} from "aurelia";
import {IQueryManager, ISession, Query} from "@domain";
import * as monaco from "monaco-editor";
import {Util} from "@common";

export class QueryView {
    @bindable public query: Query;
    @bindable public results: string;
    public id: string;
    private editor: monaco.editor.IStandaloneCodeEditor;

    constructor(
        @IQueryManager readonly queryManager: IQueryManager,
        @ISession readonly session: ISession) {
        this.id = Util.newGuid();
    }

    private attached() {
        PLATFORM.taskQueue.queueTask(() => {
            const el = document.querySelector(`[data-text-editor-id="${this.id}"]`) as HTMLElement;
            this.editor = monaco.editor.create(el, {
                value: this.query.code,
                language: 'csharp',
                theme: "vs-dark"
            });

            const f = Util.debounce(this, async (ev) => {
                await this.queryManager.updateCode(this.query.id, this.editor.getValue());
            }, 1000, true);

            this.editor.onDidChangeModelContent(ev => f(ev));

            window.addEventListener("resize", () => this.editor.layout());
            // const ob = new ResizeObserver(entries => {
            //     console.log(entries);
            //     this.editor.layout({
            //         width: document.querySelector(".window").clientWidth - document.querySelector("sidebar").clientWidth,
            //         height: document.querySelector(".text-editor").clientHeight
            //     });
            // });
            // ob.observe(document.querySelector("statusbar"));
        }, {delay: 100});
    }

    public detaching() {
        this.editor.dispose();
    }

    @watch<QueryView>(vm => vm.session.activeQuery)
    private adjustEditorLayout()
    {
        PLATFORM.taskQueue.queueTask(() => {
            this.editor.layout();
        }, {delay: 100});
    }
}
