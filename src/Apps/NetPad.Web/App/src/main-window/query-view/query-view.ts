import {bindable, PLATFORM} from "aurelia";
import {Query} from "@domain";
import * as monaco from "monaco-editor";
import {Util} from "@common";

export class QueryView {
    @bindable public query: Query;
    public id: string;
    private editor: monaco.editor.IStandaloneCodeEditor;

    constructor() {
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

            this.editor.setValue(this.query.code);

            window.addEventListener("resize", () => this.editor.layout());
            // const ob = new ResizeObserver(entries => editor.layout());
            // ob.observe(document.querySelector(".content"));
        }, { delay: 100 });
    }
}
