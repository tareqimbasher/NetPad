import {Pane} from "@application";
import {ISession, Settings} from "@domain";
import {watch} from "@aurelia/runtime-html";
import {IContainer, PLATFORM} from "aurelia";
import {OutputView} from "../../output-view/output-view";

export class OutputPane extends Pane {
    public toolbar: unknown | undefined;
    private _outputViews = new Map<string, OutputView>();

    constructor(@ISession public readonly session: ISession,
                @IContainer private readonly container: IContainer,
                private readonly settings: Settings) {
        super("Output", "run-icon");
        this.updateOutputViews();
    }

    public get outputViews() {
        return [...this._outputViews.values()];
    }

    public attached() {
        PLATFORM.queueMicrotask(() => this.activateIfApplicable());
    }

    @watch<OutputPane>(vm => vm.session.environments.length)
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

    @watch<OutputPane>(vm => vm.session.active)
    private activeChanged() {
        if (this.session.active) {
            const view = this._outputViews.get(this.session.active.script.id);
            if (view) {
                this.toolbar = view.toolbar;
                return;
            }
        }

        this.toolbar = undefined;
    }

    @watch<OutputPane>(vm => vm.session.active?.status)
    private activateIfApplicable() {
        if (this.settings.results.openOnRun && this.session.active?.status === "Running" && this.host) {
            this.activate();
        }
    }
}
