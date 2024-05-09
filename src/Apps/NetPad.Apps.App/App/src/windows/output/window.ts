import {IContainer} from "aurelia";
import {ISession} from "@domain";
import {watch} from "@aurelia/runtime-html";
import {OutputView} from "../main/output-view/output-view";
import {ResultsView} from "../main/output-view/results-view/results-view";
import {SqlView} from "../main/output-view/sql-view/sql-view";
import {WindowBase} from "@application/windows/window-base";

export class Window extends WindowBase {
    private _outputViews = new Map<string, OutputView>();
    private active?: OutputView;

    constructor(
        @ISession private readonly session: ISession,
        @IContainer private readonly container: IContainer
    ) {
        super();

        document.title = "Output";
    }

    public get outputViews() {
        return [...this._outputViews.values()];
    }

    public async binding() {
        await this.session.initialize();
    }

    public attached() {
        this.updateOutputViews();

        type OutputBroadcastMessage = {name: string, html: string};

        // Get outputs from main window
        const bc = new BroadcastChannel("output");
        bc.onmessage = (ev) => {
            const data = ev.data;
            if (!Array.isArray(data)) return;

            for (const item of data) {
                const scriptId = item.scriptId;
                const resultsOutputHtml = item.output.find((x: OutputBroadcastMessage) => x.name === nameof(ResultsView)).html;
                const sqlOutputHtml = item.output.find((x: OutputBroadcastMessage) => x.name === nameof(SqlView)).html;

                if (!scriptId) return;

                const outputView = this._outputViews.get(scriptId);
                if (!outputView) return;

                if (resultsOutputHtml) {
                    const resultsView = outputView.toolbar.options.tabs
                        .find(x => x.view instanceof ResultsView)?.view;
                    if (!resultsView) return;
                    resultsView.setHtml(resultsOutputHtml);
                }

                if (sqlOutputHtml) {
                    const sqlView = outputView.toolbar.options.tabs
                        .find(x => x.view instanceof SqlView)?.view;
                    if (!sqlView) return;
                    sqlView.setHtml(sqlOutputHtml);
                }
            }
        };

        bc.postMessage("send-outputs");
    }

    @watch<Window>(vm => vm.session.environments.length)
    private updateOutputViews() {
        const added = this.session.environments.filter(e => !this._outputViews.has(e.script.id));
        const removed = [...this._outputViews.keys()]
            .filter(id => !this.session.environments.some(e => e.script.id === id));

        for (const id of removed) {
            this._outputViews.delete(id);
        }

        for (const environment of added) {
            const view = this.container.get(OutputView);
            view.environment = environment;

            this._outputViews.set(environment.script.id, view);
        }

        this.activeChanged();
    }

    @watch<Window>(vm => vm.session.active)
    private activeChanged() {
        if (this.session.active) {
            const view = this._outputViews.get(this.session.active.script.id);
            if (view) {
                this.active = view;
                document.title = `${this.session.active?.script.name} - Output`;
                return;
            }
        }

        this.active = undefined;
    }
}
