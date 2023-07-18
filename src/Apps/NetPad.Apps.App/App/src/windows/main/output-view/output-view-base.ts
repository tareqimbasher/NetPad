import {FindTextBoxOptions, ViewModelBase} from "@application";
import {bindable, ILogger} from "aurelia";
import {HtmlScriptOutput, ScriptEnvironment} from "@domain";
import {IToolbarAction} from "./output-view-toolbar";

export abstract class OutputViewBase extends ViewModelBase {
    @bindable public environment: ScriptEnvironment;
    @bindable public active: boolean;
    public toolbarActions: IToolbarAction[];
    protected outputElement: HTMLElement;
    protected findTextBoxOptions: FindTextBoxOptions;
    protected scrollOnOutput = false;
    private lastOutputOrder = 0;
    private lastOutputElement?: Element | null;
    private pendingOutputQueue: HtmlScriptOutput[] = [];

    protected constructor(@ILogger logger: ILogger) {
        super(logger);
    }

    public bound() {
        this.findTextBoxOptions = new FindTextBoxOptions(
            this.outputElement,
            ".null, .property-value, .property-name, .text");
    }

    public getOutputHtml() {
        return this.outputElement.innerHTML;
    }

    protected appendOutput(output: HtmlScriptOutput) {
        // If output does not have an order
        if (!output.order || output.order <= 0) {
            this.appendHtml(output.body);
            return;
        }

        // If output order is not the next one that should be outputted, add it to the pending queue
        if (output.order > 0 && output.order > (this.lastOutputOrder + 1)) {
            this.pendingOutputQueue.push(output);
            return;
        }

        // Append the output
        this.lastOutputOrder = output.order;
        this.appendHtml(output.body);

        // Append any pending outputs that come after this output
        let pendingOutputIx: number;
        do {
            pendingOutputIx = this.pendingOutputQueue.findIndex(o => o.order === this.lastOutputOrder + 1);
            if (pendingOutputIx >= 0) {
                const pendingOutput = this.pendingOutputQueue[pendingOutputIx];

                this.lastOutputOrder = pendingOutput.order;
                this.appendHtml(pendingOutput.body);

                this.pendingOutputQueue.splice(pendingOutputIx, 1);
            }
        }
        while (pendingOutputIx >= 0);
    }

    protected beforeAppendOutputHtml(documentFragment: DocumentFragment) {
    }

    private appendHtml(html: string | null | undefined) {
        if (html === undefined || html === null) return;

        const template = document.createElement("template");
        template.innerHTML = html;

        this.beforeAppendOutputHtml(template.content);

        const children = Array.from(template.content.children);
        if (!children.length)
            throw new Error("Empty DocumentFragment");

        let lastChildAppendedToLastElement = false;

        for (const child of children) {
            if (this.lastOutputElement && this.shouldAppendOutputChildToLastOutputElement(child, this.lastOutputElement)) {
                this.lastOutputElement.innerHTML = this.lastOutputElement.innerHTML + child.innerHTML;
                lastChildAppendedToLastElement = true;
            } else {
                this.lastOutputElement = this.outputElement.appendChild(child);
                lastChildAppendedToLastElement = false;
            }
        }

        if (this.scrollOnOutput) {
            if (this.lastOutputElement) {
                this.lastOutputElement.scrollIntoView({block: lastChildAppendedToLastElement ? "end" : "start"});
            } else {
                this.outputElement.scrollTop = this.outputElement.scrollHeight;
            }
        }

        this.afterAppendOutputHtml();
    }

    protected afterAppendOutputHtml() {
    }

    public setHtml(html: string) {
        this.clearOutput(true);
        this.appendHtml(html);
    }

    protected beforeClearOutput() {
    }

    /**
     * Clears output.
     * @param reset set to true if output order should be reset;
     * if we're clearing in preparation for a new list/group of outputs
     * @protected
     */
    protected clearOutput(reset = false) {
        this.beforeClearOutput();
        this.lastOutputElement = null;
        this.outputElement.innerHTML = "";

        if (reset) {
            this.lastOutputOrder = 0;
            this.pendingOutputQueue.splice(0);
        }
    }

    private shouldAppendOutputChildToLastOutputElement(child: Element, lastOutputElement: Element) {
        if (!lastOutputElement || child.classList.contains("titled")) return false;

        const lastOutputIsGroupText = lastOutputElement.classList.contains("group") && lastOutputElement.classList.contains("text");
        const childIsGroupText = child.classList.contains("group") && child.classList.contains("text");

        if (!(lastOutputIsGroupText && childIsGroupText)) return false;

        if ((lastOutputElement.classList.contains("error") && !child.classList.contains("error"))
            || (!lastOutputElement.classList.contains("error") && child.classList.contains("error"))) {
            return false;
        }

        return true;
    }
}
