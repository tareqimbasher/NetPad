import {bindable, ILogger} from "aurelia";
import {
    IEventBus,
    IScriptService,
    ISession,
    IShortcutManager,
    RunScriptEvent,
    Script,
    ScriptEnvironment,
    ScriptKind,
    ScriptOutputEmitted, ScriptStatus,
    Settings
} from "@domain";
import Split from "split.js";
import {ResultsPaneSettings} from "./results-pane-settings";
import {Util} from "@common";
import {observable} from "@aurelia/runtime";

export class ScriptEnvironmentView {
    @bindable public environment: ScriptEnvironment;
    @observable public editorText: string;
    public running = false;
    public resultsPaneSettings: ResultsPaneSettings;

    private disposables: (() => void)[] = [];
    private split: Split.Instance;
    private editorTextChanged: (newText: string) => void;
    private textEditorPane: HTMLElement;
    private resultsPane: HTMLElement;
    private resultsEl: HTMLElement;


    constructor(
        readonly settings: Settings,
        @IScriptService readonly scriptService: IScriptService,
        @ISession readonly session: ISession,
        @IShortcutManager readonly shortcutManager: IShortcutManager,
        @IEventBus readonly eventBus: IEventBus,
        @ILogger readonly logger: ILogger) {
        this.resultsPaneSettings = new ResultsPaneSettings(this.settings.resultsOptions.textWrap);

        this.editorTextChanged = Util.debounce(this, async (newText: string, oldText: string) => {
            await this.sendCodeToServer();
        }, 500, true);
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

    private attached() {
        this.kind = this.script.config.kind;

        let token = this.eventBus.subscribe(RunScriptEvent, async msg => {
            if ((msg.scriptId && msg.scriptId === this.script.id) ||
                (!msg.scriptId && this.session.active.script.id === this.script.id)) {
                await this.run();
            }
        });
        this.disposables.push(() => token.dispose());

        token = this.eventBus.subscribeToServer(ScriptOutputEmitted, msg => {
            if (msg.scriptId === this.script.id) {
                this.appendResults(msg.output);
            }
        });
        this.disposables.push(() => token.dispose());

        this.split = Split([this.textEditorPane, this.resultsPane], {
            gutterSize: 6,
            direction: 'vertical',
            sizes: [100, 0],
            minSize: [50, 0],
        });
        this.disposables.push(() => this.split.destroy());
    }

    public detaching() {
        this.disposables.forEach(d => d());
    }

    public async run() {
        if (this.environment.status === "Running") return;

        try {
            await this.sendCodeToServer();
            this.setResults(null);
            if (this.settings.resultsOptions.openOnRun)
                this.openResultsPane();
            await this.scriptService.run(this.script.id);
        }
        catch (ex) {
            this.logger.error("Error while running script", ex);
        }
    }

    private async sendCodeToServer() {
        if (this.environment.script.code === this.editorText) return;
        await this.scriptService.updateCode(this.script.id, this.editorText ?? "");
    }

    private setResults(results: string | null) {
        if (!results)
            results = "";

        this.resultsEl.innerHTML = results
            .replaceAll("\n", "<br/>")
            .replaceAll(" ", "&nbsp;");
    }

    private appendResults(results: string | null) {
        this.setResults(this.resultsEl.innerHTML + results);
    }

    private openResultsPane() {
        if (this.isResultsPaneOpen()) return;
        this.split.setSizes([50, 50]);
    }

    private collapseResultsPane() {
        this.split.collapse(1);
    }

    private isResultsPaneOpen(): boolean {
        return this.resultsPane.clientHeight > 0;
    }
}
