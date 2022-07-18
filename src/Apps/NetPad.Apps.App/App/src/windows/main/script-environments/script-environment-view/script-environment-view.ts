import {bindable, ILogger} from "aurelia";
import {
    ActiveEnvironmentChangedEvent,
    IEventBus,
    IScriptService,
    ISession,
    IShortcutManager,
    RunScriptEvent,
    Script,
    ScriptEnvironment,
    ScriptKind,
    Settings
} from "@domain";
import Split from "split.js";
import {observable} from "@aurelia/runtime";
import {Editor, ViewModelBase} from "@application";

export class ScriptEnvironmentView extends ViewModelBase {
    @bindable public environment: ScriptEnvironment;
    @observable public editorText: string;
    public running = false;

    private split: Split.Instance;
    private textEditorContainer: HTMLElement;
    private resultsContainer: HTMLElement;
    private editor: () => Editor;
    private activatedAtLeastOnce = false;

    constructor(
        readonly settings: Settings,
        @IScriptService readonly scriptService: IScriptService,
        @ISession readonly session: ISession,
        @IShortcutManager readonly shortcutManager: IShortcutManager,
        @IEventBus readonly eventBus: IEventBus,
        @ILogger logger: ILogger) {
        super(logger);
    }

    public get script(): Script {
        return this.environment.script;
    }

    public get kind(): ScriptKind {
        return this.script.config.kind;
    }

    public set kind(value) {
        this.scriptService.setScriptKind(this.script.id, value);
    }

    public get isActive(): boolean {
        return this.session.active === this.environment;
    }

    public attached() {
        this.kind = this.script.config.kind;

        const runScriptEventToken = this.eventBus.subscribe(RunScriptEvent, async msg => {
            if ((msg.scriptId && msg.scriptId === this.script.id) ||
                (!msg.scriptId && this.isActive)) {
                await this.run();
            }
        });
        this.disposables.push(() => runScriptEventToken.dispose());

        const activeEnvChangedToken = this.eventBus.subscribeToServer(ActiveEnvironmentChangedEvent, message => {
            if (this.environment.script.id !== message.scriptId) {
                return;
            }

            if (!this.activatedAtLeastOnce) {
                this.activatedAtLeastOnce = true;
            }

            this.editor().focus();
        });
        this.disposables.push(() => activeEnvChangedToken.dispose());

        this.split = Split([this.textEditorContainer, this.resultsContainer], {
            gutterSize: 6,
            direction: 'vertical',
            sizes: [100, 0],
            minSize: [50, 0],
        });
        this.disposables.push(() => this.split.destroy());
    }

    private async editorTextChanged(newText: string, oldText: string) {
        await this.sendCodeToServer();
    }

    private async run() {
        if (this.environment.status === "Running") return;

        try {
            await this.sendCodeToServer();
            if (this.settings.resultsOptions.openOnRun)
                this.openResultsView();
            await this.scriptService.run(this.script.id);
        } catch (ex) {
            this.logger.error("Error while running script", ex);
        }
    }

    private async sendCodeToServer() {
        await this.scriptService.updateCode(this.script.id, this.editorText ?? "");
    }

    private openResultsView() {
        if (this.isResultsViewOpen()) return;
        this.split.setSizes([50, 50]);
    }

    private collapseResultsView = () => {
        this.split.collapse(1);
    }

    private isResultsViewOpen(): boolean {
        return this.resultsContainer.clientHeight > 10;
    }
}
