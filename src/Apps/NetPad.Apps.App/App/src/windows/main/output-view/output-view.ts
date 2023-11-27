import {bindable, Constructable, IContainer} from "aurelia";
import {ScriptEnvironment, Settings} from "@domain";
import {IToolbarAction, OutputViewToolbar, ToolbarOptions} from "./output-view-toolbar";
import {ResultsView} from "./results-view/results-view";
import {SqlView} from "./sql-view/sql-view";

export class OutputView {
    @bindable public environment: ScriptEnvironment;

    constructor(
        public readonly toolbar: OutputViewToolbar,
        public readonly settings: Settings,
        @IContainer private readonly container: IContainer) {
    }

    public get font(): string {
        return this.settings.results.font ? this.settings.results.font : "inherit";
    }

    public bound() {
        const self = this;
        this.toolbar.options = new ToolbarOptions(
            [
                {
                    text: "Results",
                    view: this.initView(ResultsView),
                    clicked: async function () {
                        self.setToolbarActions(this.view.toolbarActions);
                    },
                    active: true
                },
                {
                    text: "SQL",
                    view: this.initView(SqlView),
                    clicked: async function () {
                        self.setToolbarActions(this.view.toolbarActions);
                    },
                }
            ],
            []
        );

        this.setToolbarActions(this.toolbar.options.tabs[0].view.toolbarActions);
    }

    private setToolbarActions(actions: IToolbarAction[]) {
        this.toolbar.options.actions.splice(0);
        this.toolbar.options.actions.push(...actions);
    }

    private initView<TView extends Constructable<ResultsView | SqlView>>(type: TView): InstanceType<TView> {
        const view = this.container.get(type);
        view.environment = this.environment;
        return view as InstanceType<TView>;
    }
}
