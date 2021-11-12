import {bindable, PLATFORM} from "aurelia";
import {IQueryManager, Query} from "@domain";
import * as monaco from "monaco-editor";
import {Util} from "@common";

export class QueryView {
    @bindable public query: Query;
    @bindable public results: string;
    public id: string;
    private editor: monaco.editor.IStandaloneCodeEditor;

    constructor(@IQueryManager readonly queryManager: IQueryManager) {
        this.id = Util.newGuid();
    }

    private attached() {
        PLATFORM.taskQueue.queueTask(() => {
            const el = document.querySelector(`[data-text-editor-id="${this.id}"]`) as HTMLElement;
            this.editor = monaco.editor.create(el, {
                value: 'Console.WriteLine("Hello World");',
                language: 'csharp',
                theme: "vs-dark"
            });

            this.editor.onDidChangeModelContent(ev => Util.debounce(this, async (ev) => {
                await this.queryManager.updateCode(this.query.id, this.editor.getValue());
            }, 1000)(ev));

            window.addEventListener("resize", () => this.editor.layout());
            // const ob = new ResizeObserver(entries => editor.layout());
            // ob.observe(document.querySelector(".content"));
        }, {delay: 100});
    }
}
