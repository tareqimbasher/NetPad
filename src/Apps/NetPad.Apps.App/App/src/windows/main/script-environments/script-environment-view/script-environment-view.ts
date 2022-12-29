import {bindable, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {
    ActiveEnvironmentChangedEvent,
    DataConnection,
    DataConnectionStore,
    IEventBus,
    IScriptService,
    ISession,
    RunOptionsDto,
    RunScriptEvent,
    Script,
    ScriptEnvironment,
    ScriptKind, ScriptStatus,
    Settings
} from "@domain";
import Split from "split.js";
import {observable} from "@aurelia/runtime";
import {Editor, IShortcutManager, ViewModelBase} from "@application";

export class ScriptEnvironmentView extends ViewModelBase {
    @bindable public environment: ScriptEnvironment;

    @observable public editorText: string;
    public running = false;

    private split: Split.Instance;
    private textEditorContainer: HTMLElement;
    private outputContainer: HTMLElement;
    private editor: Editor;
    private activatedAtLeastOnce = false;

    constructor(
        private readonly settings: Settings,
        @IScriptService private readonly scriptService: IScriptService,
        @ISession private readonly session: ISession,
        private readonly dataConnectionStore: DataConnectionStore,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @IEventBus private readonly eventBus: IEventBus,
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
        this.scriptService.setScriptKind(this.script.id, value)
            .catch(err => {
                this.logger.error("Failed to set script kind", err);
            });
    }

    public get dataConnection(): DataConnection | undefined {
        if (!this.script.dataConnection)
            return undefined;

        // We want to return the connection object from the connection store, not the connection
        // defined in the script.dataConnection property because they both reference 2 different
        // object instances, even though they are "the same connection"
        return this.dataConnectionStore.connections.find(c => c.id == this.script.dataConnection?.id);
    }

    public set dataConnection(value: DataConnection | undefined) {
        this.scriptService.setDataConnection(this.script.id, value?.id)
            .catch(err => {
                this.logger.error("Failed to set script data connection", err);
            });
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

            this.editor.focus();
        });
        this.disposables.push(() => activeEnvChangedToken.dispose());

        this.split = Split([this.textEditorContainer, this.outputContainer], {
            gutterSize: 6,
            direction: 'vertical',
            sizes: [100, 0],
            minSize: [50, 0],
        });
        this.disposables.push(() => this.split.destroy());

        if (this.environment.status === "Running")
            this.openResultsView();
    }

    private async editorTextChanged(newText: string, oldText: string) {
        await this.sendCodeToServer();
    }

    private async run() {
        if (this.environment.status === "Running") return;

        try {
            await this.sendCodeToServer();

            const runOptions = new RunOptionsDto();

            // If user has code selected, only run selection
            const selection = this.editor.monacoEditor?.getSelection();
            if (selection && !selection.isEmpty()) {
                runOptions.specificCodeToRun = this.editor.monacoEditor?.getModel()?.getValueInRange(selection);
            }

            await this.scriptService.run(this.script.id, runOptions);
        } catch (ex) {
            this.logger.error("Error while running script", ex);
        }
    }

    private async stop() {
        if (this.environment.status !== "Running") return;

        try {
            await this.scriptService.stop(this.script.id);
        } catch (ex) {
            this.logger.error("Error while stopping script", ex);
        }
    }

    private async sendCodeToServer() {
        await this.scriptService.updateCode(this.script.id, this.editorText ?? "");
    }

    private openResultsView() {
        if (this.isResultsViewOpen()) return;
        this.split.setSizes([30, 70]);
    }

    private collapseResultsView = () => {
        this.split.collapse(1);
    }

    private isResultsViewOpen(): boolean {
        return this.outputContainer.clientHeight > 10;
    }

    @watch<ScriptEnvironmentView>(vm => vm.environment.status)
    private environmentStatusChanged(newStatus: ScriptStatus) {
        if (this.settings.results.openOnRun && newStatus === "Running" && !this.isResultsViewOpen()) {
            this.openResultsView();
        }
    }
}
