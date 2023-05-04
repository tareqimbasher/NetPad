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

        const rvs = this.resultsViewSettings;
        this.toolbarActions = [
            {
                label: "Format",
                actions: [
                    {
                        label: "Collapse to Level 1",
                        clicked: async () => this.resultControls.collapseAll(1)
                    },
                    {
                        label: "Collapse to Level 2",
                        clicked: async () => this.resultControls.collapseAll(2)
                    },
                    {
                        label: "Collapse to Level 3",
                        clicked: async () => this.resultControls.collapseAll(3)
                    },
                    {
                        label: "Expand to Level 1",
                        clicked: async () => this.resultControls.expandAll(1)
                    },
                    {
                        label: "Expand to Level 2",
                        clicked: async () => this.resultControls.expandAll(2)
                    },
                    {
                        label: "Expand to Level 3",
                        clicked: async () => this.resultControls.expandAll(3)
                    }
                ]
            },
            {
                label: "Collapse All",
                icon: "tree-collapse-all-icon",
                clicked: async () => this.resultControls.collapseAll()
            },
            {
                label: "Expand All",
                icon: "tree-expand-all-icon",
                clicked: async () => this.resultControls.expandAll()
            },
            {
                label: "Text Wrap",
                icon: "text-wrap-icon",
                active: this.resultsViewSettings.textWrap,
                clicked: async function () {
                    rvs.textWrap = !rvs.textWrap;
                    this.active = rvs.textWrap;
                },
            },
            {
                label: "Clear",
                icon: "clear-output-icon",
                clicked: async () => this.clearOutput(),
            },
        ];
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
            this.clearOutput(true);
    }
}
