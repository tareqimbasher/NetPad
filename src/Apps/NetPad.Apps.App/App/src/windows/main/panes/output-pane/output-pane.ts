import {IShortcutManager, Pane, PaneAction} from "@application";
import {ISession, IWindowService, Settings} from "@domain";
import {watch} from "@aurelia/runtime-html";
import {IContainer, PLATFORM} from "aurelia";
import {OutputView} from "../../output-view/output-view";
import {AppWindows} from "@application/windows/app-windows";

export class OutputPane extends Pane {
    public toolbar: unknown | undefined;
    private _outputViews = new Map<string, OutputView>();

    constructor(@ISession public readonly session: ISession,
                @IWindowService private readonly windowService: IWindowService,
                @IContainer private readonly container: IContainer,
                private readonly appWindows: AppWindows,
                @IShortcutManager private readonly shortcutManager: IShortcutManager,
                private readonly settings: Settings) {
        super("Output", "run-icon");

        this.hasShortcut(shortcutManager.getShortcutByName("Output"));

        this._actions.push(new PaneAction(
            '<i class="pop-out-icon me-3"></i> Pop out',
            "Pop out into a new window",
            async () => {
                this.hide();
                await this.windowService.openOutputWindow();
            }
        ))
    }

    public get outputViews() {
        return [...this._outputViews.values()];
    }

    public attached() {
        this.updateOutputViews();
        PLATFORM.queueMicrotask(() => this.activateIfApplicable());

        const bc = new BroadcastChannel("output");
        bc.onmessage = (ev) => {
            if (ev.data !== "send-outputs") return;

            const outputs = this.outputViews.map(ov => {
                return {
                    scriptId: ov.environment.script.id,
                    output: ov.toolbar.options.tabs.map(t => {
                        return {
                            name: t.view.constructor.name,
                            html: t.view.getOutputHtml()
                        };
                    })
                };
            });

            bc.postMessage(outputs);
        };
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
        if (this.appWindows.items.find(x => x.name === "output")) return;

        if (this.settings.results.openOnRun && this.session.active?.status === "Running") {
            this.activate();
        }
    }

    @watch<OutputPane>(vm => vm.appWindows.items.map(x => x.name))
    private activateIfPopoutWindowClosed(newValue, oldValue) {
        const existedInOld = oldValue.indexOf("output") >= 0;
        const existsInNew = newValue.indexOf("output") >= 0;

        if (existedInOld && !existsInNew) {
            this.activate();
        } else if (existsInNew) {
            this.hide();
        }
    }
}
