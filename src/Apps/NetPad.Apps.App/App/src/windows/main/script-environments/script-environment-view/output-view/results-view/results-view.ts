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
        this.addDisposable(() => this.resultControls.dispose());

        const token = this.eventBus.subscribeToServer(ScriptOutputEmittedEvent, msg => {
            if (msg.scriptId === this.environment.script.id) {
                if (!msg.output) return;

                const output = JSON.parse(msg.output) as HtmlScriptOutput;
                this.appendOutput(output);
            }
        });
        this.addDisposable(() => token.dispose());
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
}
