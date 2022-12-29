import {ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {ResultsPaneViewSettings} from "./results-view-settings";
import {HtmlScriptOutput, IEventBus, ISession, ScriptOutputEmittedEvent, ScriptStatus, Settings} from "@domain";
import {ResultControls} from "./result-controls";
import {OutputViewBase} from "../output-view-base";

export class ResultsView extends OutputViewBase {
    public resultsViewSettings: ResultsPaneViewSettings;
    private resultControls: ResultControls;

    constructor(private readonly settings: Settings,
                @ISession private readonly session: ISession,
                @IEventBus readonly eventBus: IEventBus,
                @ILogger logger: ILogger
    ) {
        super(logger);
        this.resultsViewSettings = new ResultsPaneViewSettings(this.settings.results.textWrap);
    }

    public attached() {
        this.resultControls = new ResultControls(this.outputElement);
        this.disposables.push(() => this.resultControls.dispose());

        const token = this.eventBus.subscribeToServer(ScriptOutputEmittedEvent, msg => {
            if (msg.scriptId === this.environment.script.id) {
                if (!msg.output) return;

                const output = JSON.parse(msg.output) as HtmlScriptOutput;
                this.appendOutput(output);
            }
        });
        this.disposables.push(() => token.dispose());
    }

    protected override beforeAppendOutputHtml(documentFragment: DocumentFragment) {
        this.resultControls.bind(documentFragment);
    }

    protected override beforeClearOutput() {
        this.resultControls.dispose();
    }

    @watch<ResultsView>(vm => vm.environment.status)
    private scriptStatusChanged(newStatus: ScriptStatus, oldStatus: ScriptStatus) {
        if (oldStatus !== "Running" && newStatus === "Running")
            this.clearOutput();
    }

    private attemptsAtWrappingEachResultLetterWithSpan() {
        // Was going to be used for search functionality, but performance of inserting
        // potentially millions of span elements in the page proved to be very poor

        // Array.from(template.content.querySelectorAll("*")).forEach(el => {
        //     const currentInnerHtml = el.innerHTML;
        //     let newInnerHtml = "";
        //     let isInsideXml = false;
        //
        //     for (let iChar = 0; iChar < currentInnerHtml.length; iChar++) {
        //         const char = currentInnerHtml[iChar];
        //
        //         if (char === "<") {
        //             isInsideXml = true;
        //         }
        //
        //         if (isInsideXml) {
        //             newInnerHtml += char;
        //             if (char === ">") isInsideXml = false;
        //             continue;
        //         }
        //
        //         if (char === " ")
        //             newInnerHtml += char;
        //         else {
        //             newInnerHtml +=
        //                 `<span class="${char}">`
        //                 + char
        //                 + '</span>';
        //         }
        //     }
        //
        //     el.innerHTML = newInnerHtml;
        // });


        //template.normalize();
        // Array.from(template.content.querySelectorAll("*")).forEach(el => {
        //     const textNodes = Array.from(el.childNodes).filter(n => n.nodeType === Node.TEXT_NODE) as Text[];
        //     textNodes.forEach(n => {
        //         if (!n.data) return;
        //         const replacements: Element[] = [];
        //         for (let iChar = 0; iChar < n.data.length; iChar++) {
        //             const char = n.data[iChar];
        //             if (char === " ") continue;
        //
        //             try {
        //                 const span = document.createElement("span");
        //                 span.classList.add(char.toLowerCase());
        //                 span.innerText = char;
        //                 replacements.push(span);
        //             } catch (ex) {
        //                 console.error(`Failed to replace char '${char}'`, n, ex);
        //             }
        //         }
        //         n.replaceWith(...replacements);
        //     });
        // });
    }
}
