import {ResultsPaneViewSettings} from "./results-view-settings";
import {IEventBus, ISession, ScriptEnvironment, ScriptOutputEmittedEvent, ScriptStatus, Settings} from "@domain";
import {bindable, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {ViewModelBase} from "@application";
import {ResultControls} from "./result-controls";

export class ResultsView extends ViewModelBase {
    public resultsViewSettings: ResultsPaneViewSettings;
    @bindable public environment: ScriptEnvironment;
    @bindable public active: boolean;

    private outputElement: HTMLElement;
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

        const token = this.eventBus.subscribeToServer(ScriptOutputEmittedEvent, msg => {
            if (msg.scriptId === this.environment.script.id) {
                this.appendResults(msg.output);
            }
        });
        this.disposables.push(() => token.dispose());
    }

    public override detaching() {
        this.resultControls.dispose();
        super.detaching();
    }

    private appendResults(results: string | null | undefined) {
        if (!results) return;

        const template = document.createElement("template");
        template.innerHTML = results;
        this.resultControls.bind(template.content);
        this.outputElement.appendChild(template.content);
    }

    private clearResults() {
        this.resultControls.dispose();
        while (this.outputElement.firstChild && this.outputElement.lastChild)
            this.outputElement.removeChild(this.outputElement.lastChild);
    }

    @watch<ResultsView>(vm => vm.environment.status)
    private scriptStatusChanged(newStatus: ScriptStatus, oldStatus: ScriptStatus) {
        if (oldStatus !== "Running" && newStatus === "Running")
            this.clearResults();
    }
}

