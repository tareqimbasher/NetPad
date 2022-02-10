import {ResultsPaneViewSettings} from "./results-view-settings";
import {IEventBus, ISession, ScriptEnvironment, ScriptOutputEmitted, ScriptStatus, Settings} from "@domain";
import {bindable, IDisposable, watch} from "aurelia";

export class ResultsView {
    public resultsViewSettings: ResultsPaneViewSettings;
    @bindable public environment: ScriptEnvironment;
    @bindable public onCloseRequested: () => void;

    private resultsEl: HTMLElement;
    private resultControls: ResultControls;
    private disposables: (() => void)[] = [];

    constructor(private readonly settings: Settings,
                @ISession private readonly session: ISession,
                @IEventBus readonly eventBus: IEventBus,
    ) {
        this.resultsViewSettings = new ResultsPaneViewSettings(this.settings.resultsOptions.textWrap);
    }

    public attached() {
        this.resultControls = new ResultControls(this.resultsEl);

        const token = this.eventBus.subscribeToServer(ScriptOutputEmitted, msg => {
            if (msg.scriptId === this.environment.script.id) {
                this.appendResults(msg.output);
            }
        });
        this.disposables.push(() => token.dispose());
    }

    public detaching() {
        this.resultControls.dispose();
        this.disposables.forEach(d => d());
    }

    private appendResults(results: string | null) {
        if (!results) return;

        const template = document.createElement("template");
        template.innerHTML = results;
        this.resultControls.bind(template.content);
        this.resultsEl.appendChild(template.content);
    }

    private clearResults()
    {
        this.resultControls.dispose();
        while (this.resultsEl.firstChild)
            this.resultsEl.removeChild(this.resultsEl.lastChild);
    }

    @watch<ResultsView>(vm => vm.environment.status)
    private scriptStatusChanged(newStatus: ScriptStatus, oldStatus: ScriptStatus) {
        if (oldStatus !== "Running" && newStatus === "Running")
            this.clearResults();
    }
}

class ResultControls implements IDisposable {
    private disposables: (() => void)[] = [];

    constructor(private readonly resultsElement: HTMLElement) {
    }

    public bind(content: DocumentFragment) {

        for (const table of Array.from(content.querySelectorAll("table"))) {

            let collapseTarget = table.querySelector(":scope > thead > tr > th.table-item-count");
            if (!collapseTarget)
                collapseTarget = table.querySelector(":scope > thead > tr > th");

            collapseTarget.classList.add("collapse-actionable");
            const clickHandler = () => this.toggle(table);
            collapseTarget.addEventListener("click", clickHandler);
            this.disposables.push(() => collapseTarget.removeEventListener("click", clickHandler));

            const caret = document.createElement("i");
            collapseTarget.prepend(caret);
            caret.classList.add("bi", "bi-caret-down-fill", "me-2");
        }
    }

    public expand(element: Element) {
        element.classList.remove("collapsed");
    }

    public collapse(element: Element) {
        element.classList.add("collapsed");
    }

    public toggle(element: Element) {
        if (element.classList.contains("collapsed"))
            this.expand(element);
        else
            this.collapse(element);
    }

    private expandAllTables() {
        this.querySelectorAll("table").forEach(t => this.expand(t));
    }

    private collapseAllTables() {
        this.querySelectorAll("table").forEach(t => this.collapse(t));
    }

    querySelectorAll(selectors: string) {
        return Array.from(this.resultsElement.querySelectorAll(selectors))
    }

    public dispose(): void {
        this.disposables.forEach(d => d());
    }
}
