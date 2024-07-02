import {ScriptOutput, Settings} from "@application";
import {DisposableCollection, IDisposable, KeyCode, Util} from "@common";
import {ResultControls} from "./result-controls";
import {NavigationControls} from "./navigation-controls";

export class DumpContainer implements IDisposable {
    public readonly element: HTMLElement;
    public navigationControls: NavigationControls;
    public resultControls: ResultControls;
    public scrollOnOutput = false;
    public textWrap = false;
    public lastOutputOrder = 0;             // The order of the last output message rendered

    private renderQueue: Element[] = [];
    private lastRenderedOutput?: Element | null;

    // A queue to temporarily park output messages that should not be rendered yet because
    // they are not in the correct order and should wait to be rendered until previously emitted
    // messages arrive.
    private earlyMessagesOutputQueue: ScriptOutput[] = [];
    private scrollTop = 0;
    private disposables = new DisposableCollection();

    constructor(settings: Settings) {
        this.element = document.createElement("div");
        this.element.classList.add("dump-container");
        this.element.tabIndex = 0;
        this.disposables.add(() => this.element.remove());

        this.textWrap = settings.results.textWrap;

        this.navigationControls = new NavigationControls(this.element);

        this.resultControls = new ResultControls(this.element);
        this.disposables.add(() => this.resultControls.dispose());

        // Confine select all to the container
        const selectAllKeyHandler = (ev: KeyboardEvent) => {
            if (ev.code === KeyCode.KeyA && (ev.ctrlKey || ev.metaKey)) {
                const range = document.createRange();
                range.selectNode(this.element);
                window.getSelection()?.removeAllRanges();
                window.getSelection()?.addRange(range);

                ev.preventDefault();
            }
        };

        this.element.addEventListener("keydown", selectAllKeyHandler);
        this.disposables.add(() => this.element.removeEventListener("keydown", selectAllKeyHandler));
    }


    public attachedToDom() {
        if (this.element.parentElement) {
            this.element.parentElement.scrollTop = this.scrollTop;
        }
    }

    public detachingFromDom() {
        this.scrollTop = this.element.parentElement?.scrollTop ?? 0;
    }

    public getHtml() {
        return this.element.innerHTML;
    }

    public setHtml(html: string) {
        this.clearOutput(true);
        this.appendHtml(html);
    }

    public appendOutput(output: ScriptOutput) {
        // If output does not have an order
        if (!output.order || output.order <= 0) {
            this.appendHtml(output.body);
            return;
        }

        // If output order is not the next one that should be outputted, add it to the pending queue
        if (output.order > 0 && output.order > (this.lastOutputOrder + 1)) {
            this.earlyMessagesOutputQueue.push(output);
            return;
        }

        // Append the output
        this.lastOutputOrder = output.order;
        this.appendHtml(output.body);

        // Append any "early" outputs that come after this output
        let earlyOutputIx: number;
        do {
            earlyOutputIx = this.earlyMessagesOutputQueue.findIndex(o => o.order === this.lastOutputOrder + 1);
            if (earlyOutputIx >= 0) {
                const pendingOutput = this.earlyMessagesOutputQueue[earlyOutputIx];

                this.lastOutputOrder = pendingOutput.order;
                this.appendHtml(pendingOutput.body);

                this.earlyMessagesOutputQueue.splice(earlyOutputIx, 1);
            }
        }
        while (earlyOutputIx >= 0);
    }

    protected mutateHtmlBeforeAppend(html: string) {
        return html;
    }

    protected beforeAppendHtml(documentFragment: DocumentFragment) {
        this.resultControls.bind(documentFragment);
    }

    private appendHtml(html: string | null | undefined) {
        if (!html) {
            return;
        }

        html = this.mutateHtmlBeforeAppend(html);

        const template = document.createElement("template");
        template.innerHTML = html;

        this.beforeAppendHtml(template.content);

        const children = Array.from(template.content.children);
        if (!children.length)
            throw new Error("Empty DocumentFragment");

        let htmlToAppendToLastRenderedOutput: string = "";

        for (const child of children) {
            if (this.lastRenderedOutput && this.shouldAppendOutputChildToLastRenderedOutput(child, this.lastRenderedOutput)) {
                const childHtml = child.innerHTML;
                htmlToAppendToLastRenderedOutput += childHtml;
            } else {
                this.lastRenderedOutput = child;
                this.renderQueue.push(child);
            }
        }

        if (htmlToAppendToLastRenderedOutput) {
            this.lastRenderedOutput!.innerHTML = this.lastRenderedOutput!.innerHTML + htmlToAppendToLastRenderedOutput;

            if (this.scrollOnOutput) {
                this.navigationControls.navigateBottom();
            }

            this.afterAppendHtml();
        }

        this.processRenderQueue();
    }

    protected afterAppendHtml() {
    }

    private processRenderQueue = Util.debounce(this, () => {
        const batch = [...this.renderQueue.splice(0)];

        for (let iEl = 0; iEl < batch.length; iEl++) {
            const group = batch[iEl];

            if (group.classList.contains("code")) {
                const codeEl = group.querySelector("code");
                if (codeEl) {
                    const lang = codeEl.getAttribute("language");
                    const code = codeEl.textContent ?? "";

                    if (code) {
                        import("highlight.js/lib/common") // remove to a prop
                            .then(m => m.default)
                            .then(hljs => {
                                codeEl.innerHTML = !lang || lang === "auto" || !hljs.autoDetection(lang)
                                    ? hljs.highlightAuto(code).value
                                    : hljs.highlight(code, {language: lang}).value;
                            });
                    }
                }
            } else if (group.lastElementChild?.tagName.toLowerCase() === "script") {
                // Script tags cannot be injected as is, they must be recreated and appended to the DOM for
                // them to execute.
                const script = document.createElement("script");
                const code = document.createTextNode(group.textContent ?? "");
                script.appendChild(code);

                // Replace the previous script
                group.lastElementChild.remove();
                group.appendChild(script);
            }
        }

        if (batch.length === 0) {
            return;
        }

        this.element.append(...batch);

        if (this.scrollOnOutput) {
            this.navigationControls.navigateBottom()
        }

        this.afterAppendHtml();
    }, 5);

    protected beforeClearOutput() {
        this.resultControls.dispose();
    }

    /**
     * Clears output.
     * @param reset set to true if output order should be reset;
     * if we're clearing in preparation for a new list/group of outputs
     */
    public clearOutput(reset = false) {
        this.beforeClearOutput();
        this.lastRenderedOutput = null;
        this.renderQueue.splice(0);
        this.element.innerHTML = "";

        if (reset) {
            this.lastOutputOrder = 0;
            this.earlyMessagesOutputQueue.splice(0);
        }
    }

    private shouldAppendOutputChildToLastRenderedOutput(child: Element, lastRenderedOutput: Element) {
        if (!lastRenderedOutput || child.classList.contains("titled")) return false;

        const lastOutputIsInlinableGroupText = lastRenderedOutput.classList.contains("group")
            && lastRenderedOutput.classList.contains("text")
            && lastRenderedOutput.lastElementChild?.tagName.toLowerCase() !== "br";

        const childIsGroupText = child.classList.contains("group") && child.classList.contains("text");

        if (!(lastOutputIsInlinableGroupText && childIsGroupText)) return false;

        if ((lastRenderedOutput.classList.contains("error") && !child.classList.contains("error"))
            || (!lastRenderedOutput.classList.contains("error") && child.classList.contains("error"))) {
            return false;
        }

        return true;
    }

    public dispose() {
        this.disposables.dispose();
    }
}
