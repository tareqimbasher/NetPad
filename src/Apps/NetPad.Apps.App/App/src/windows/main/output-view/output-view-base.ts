import {FindTextBoxOptions, ViewModelBase} from "@application";
import {bindable, ILogger} from "aurelia";
import {ScriptEnvironment, ScriptOutput} from "@domain";
import {IToolbarAction} from "./output-view-toolbar";
import {Util} from "@common";

export abstract class OutputViewBase extends ViewModelBase {
    @bindable public environment: ScriptEnvironment;
    @bindable public active: boolean;
    public toolbarActions: IToolbarAction[];
    protected findTextBoxOptions: FindTextBoxOptions;
    protected scrollOnOutput = false;

    // The HTML element that all output will be rendered inside of
    protected outputElement: HTMLElement;

    // The current (approximate) length of the outputElement's inner HTML property
    private outputElementLength = 0;

    // The current number of children in the outputElement
    private outputElementChildCount = 0;

    // The last output added to the outputElement
    private lastRenderedOutput?: Element | null;

    // A queue to temporarily park output messages that should not be rendered yet because
    // they are not in the correct order and should wait to be rendered until previously emitted
    // messages arrive.
    private earlyMessagesOutputQueue: ScriptOutput[] = [];

    // The order of the last output message rendered
    private lastOutputOrder = 0;

    // A queue where elements await being appended to the outputElement
    protected renderQueue: Element[] = [];

    private processRenderQueue = Util.debounce(this, () => {
        this.outputElement.append(...this.renderQueue);
        this.renderQueue.splice(0);

        if (this.scrollOnOutput) {
            this.outputElement.scrollTop = this.outputElement.scrollHeight;
        }
    }, 5);

    protected constructor(logger: ILogger) {
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

    protected appendOutput(output: ScriptOutput) {
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

    protected beforeAppendOutputHtml(documentFragment: DocumentFragment) {
    }

    private appendHtml(html: string | null | undefined) {
        if (html === undefined || html === null ) return;

        const template = document.createElement("template");
        template.innerHTML = html;

        this.beforeAppendOutputHtml(template.content);

        const children = Array.from(template.content.children);
        if (!children.length)
            throw new Error("Empty DocumentFragment");

        this.outputElementChildCount += children.length;

        for (const child of children) {
            if (this.lastRenderedOutput && this.shouldAppendOutputChildToLastRenderedOutput(child, this.lastRenderedOutput)) {
                const childInnerHtml = child.innerHTML;
                this.outputElementLength += childInnerHtml.length;
                this.lastRenderedOutput.innerHTML = this.lastRenderedOutput.innerHTML + childInnerHtml;

                if (this.scrollOnOutput) {
                    this.outputElement.scrollTop = this.outputElement.scrollHeight;
                }
            } else {
                this.outputElementLength += child.innerHTML.length;
                this.lastRenderedOutput = child;
                this.renderQueue.push(child);
                this.processRenderQueue();
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
        this.lastRenderedOutput = null;
        this.renderQueue.splice(0);
        this.outputElement.innerHTML = "";
        this.outputElementLength = 0;
        this.outputElementChildCount = 0;

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
}
