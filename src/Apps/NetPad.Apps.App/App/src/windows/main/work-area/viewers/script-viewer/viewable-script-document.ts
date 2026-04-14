import {
    EnvironmentPropertyChangedEvent,
    IAppService,
    IEventBus,
    IScriptService,
    ISession,
    RunOptions,
    ScriptCodeUpdatedEvent,
    ScriptConfigPropertyChangedEvent,
    ScriptEnvironment,
    ScriptKind,
    ScriptPropertyChangedEvent,
} from "@application";
import {ViewableStatusIndicator} from "../viewable-object";
import {ViewableTextDocument} from "../viewable-text-document";
import {ViewerHost} from "../viewer-host";
import {IWorkAreaService} from "../../work-area-service";
import {TextLanguage} from "@application/editor/text-language";
import {TextDocument} from "@application/editor/text-document";
import {DndType} from "@application/dnd/dnd-type";
import {DragAndDropBase} from "@application/dnd/drag-and-drop-base";
import {DataConnectionDnd} from "@application/dnd/data-connection-dnd";
import {RunScriptCommand} from "@application/scripts/run-script-command";

export class ViewableScriptDocument extends ViewableTextDocument {
    constructor(
        public readonly environment: ScriptEnvironment,
        private readonly scriptService: IScriptService,
        private readonly session: ISession,
        private readonly appService: IAppService,
        private readonly workAreaService: IWorkAreaService,
        eventBus: IEventBus
    ) {
        super(
            environment.script.id,
            environment.script.name,
            ViewableScriptDocument.getLanguageFromScriptKind(environment.script.config.kind),
            environment.script.code,
        );

        this.refreshDisplayProperties();

        this.addDisposable(
            eventBus.subscribeToServer(ScriptCodeUpdatedEvent, ev => {
                if (ev.scriptId !== this.environment.script.id) {
                    return;
                }

                this.textDocument.setText("server", ev.newCode ?? "");
            })
        );

        this.addDisposable(
            eventBus.subscribeToServer(ScriptConfigPropertyChangedEvent, ev => {
                if (ev.scriptId !== this.environment.script.id) {
                    return;
                }

                if (ev.propertyName === "Kind") {
                    if (ev.newValue == "Program") this.textDocument.changeLanguage("csharp");
                    else if (ev.newValue == "SQL") this.textDocument.changeLanguage("sql");
                    this.iconImageSrc = ViewableScriptDocument.resolveIconImageSrc(ev.newValue as ScriptKind);
                }
            })
        );

        this.addDisposable(
            eventBus.subscribeToServer(EnvironmentPropertyChangedEvent, ev => {
                if (ev.scriptId !== this.environment.script.id) {
                    return;
                }

                if (ev.propertyName === "Status" || ev.propertyName === "RunDurationMilliseconds") {
                    this.updateStatusIndicator();
                }
            })
        );

        this.addDisposable(
            eventBus.subscribeToServer(ScriptPropertyChangedEvent, ev => {
                if (ev.scriptId !== this.environment.script.id) {
                    return;
                }

                if (ev.propertyName === "Path") {
                    this.path = this.environment.script.path;
                    this.updateTooltip();
                } else if (ev.propertyName === "DataConnection") {
                    this.updateSubtitle();
                } else if (ev.propertyName === "Name") {
                    this.updateTooltip();
                }
            })
        );

        // Per-viewable handler for RunScriptCommand when a specific scriptId is provided.
        // The "no scriptId → run active viewable" case lives in WorkArea (will move to
        // WorkAreaService in phase 5).
        this.addDisposable(
            eventBus.subscribe(RunScriptCommand, async (msg: RunScriptCommand) => {
                if (msg.scriptId === this.environment.script.id) {
                    await this.run();
                }
            })
        );
    }

    private static getLanguageFromScriptKind(kind: ScriptKind): TextLanguage {
        if (kind == "Program") return "csharp";
        else if (kind === "SQL") return "sql";
        else throw new Error("Unhandled script kind: " + kind);
    }

    private static resolveIconImageSrc(kind: ScriptKind): string | undefined {
        if (kind === "Program" || kind === "Expression") return "img/csharp-logo.png";
        if (kind === "SQL") return "img/sql-logo.svg";
        return undefined;
    }

    public override get name() {
        return this.environment.script.name;
    }

    public override get isDirty() {
        return this.environment.script.isDirty;
    }

    public get script() {
        return this.environment.script;
    }

    public override get textDocument(): TextDocument {
        const alreadyCreated = this._textDocument !== null && this._textDocument !== undefined;
        const textDocument = super.textDocument;

        if (!alreadyCreated) {
            this.addDisposable(
                textDocument.onChange(async (setter) => {
                    if (setter === "server") return;
                    await this.textChanged();
                }));
        }

        return textDocument;
    }

    // --- ViewableObject overrides ---

    public override open(viewerHost: ViewerHost): Promise<void> {
        viewerHost.addViewables(this);
        return Promise.resolve();
    }

    public override async close(viewerHost: ViewerHost): Promise<void> {
        const openInOtherViewerHosts = this.workAreaService.viewerHosts.items
            .find(x => x !== viewerHost && x.viewables.has(this));

        if (openInOtherViewerHosts) {
            viewerHost.removeViewables(this);
        } else if (this.environment.status !== "Running" && this.environment.status !== "Stopping") {
            await this.session.close(this.environment.script.id, false);
        }
    }

    public override async activate(_viewerHost: ViewerHost): Promise<void> {
        await this.session.activate(this.environment.script.id);
    }

    public override canSave(): boolean {
        return true;
    }

    public override save(): Promise<boolean> {
        return this.scriptService.save(this.environment.script.id);
    }

    public override canRename(): boolean {
        return true;
    }

    public override rename(): Promise<void> {
        return this.scriptService.openRenamePrompt(this.environment.script);
    }

    public override canDuplicate(): boolean {
        return true;
    }

    public override async duplicate(): Promise<void> {
        await this.scriptService.duplicate(this.environment.script.id);
    }

    public override canOpenContainingFolder(): boolean {
        return !!this.environment.script.path;
    }

    public override openContainingFolder(): Promise<void> {
        const path = this.environment.script.path;
        if (!path) {
            return Promise.reject("Script has not been saved yet");
        }
        return this.appService.openFolderContainingScript(path);
    }

    public override canRun(): boolean {
        return this.statusIndicator !== "running" && this.statusIndicator !== "stopping";
    }

    public override async run(): Promise<void> {
        const document = this.textDocument;
        const runOptions = new RunOptions();

        if (document.selection && !document.selection.isEmpty()) {
            runOptions.specificCodeToRun = document.textModel.getValueInRange(document.selection);
        }

        await this.scriptService.run(this.environment.script.id, runOptions);
    }

    public override canStop(): boolean {
        return this.statusIndicator === "running";
    }

    public override async stop(): Promise<void> {
        await this.scriptService.stop(this.environment.script.id, true);
    }

    public override canOpenProperties(): boolean {
        return true;
    }

    public override async openProperties(): Promise<void> {
        await this.scriptService.openConfigWindow(this.environment.script.id, null);
    }

    public override canHandleDrop(dnd: DragAndDropBase | null | undefined): boolean {
        return dnd?.type === DndType.DataConnection;
    }

    public override async handleDrop(dnd: DragAndDropBase): Promise<void> {
        if (dnd?.type === DndType.DataConnection) {
            await this.scriptService.setDataConnection(
                this.environment.script.id,
                (dnd as DataConnectionDnd).dataConnectionId
            );
        }
    }

    // --- Display property helpers ---

    private refreshDisplayProperties(): void {
        this.iconImageSrc = ViewableScriptDocument.resolveIconImageSrc(this.environment.script.config.kind);
        this.path = this.environment.script.path;
        this.updateSubtitle();
        this.updateTooltip();
        this.updateStatusIndicator();
    }

    private updateSubtitle(): void {
        const connection = this.environment.script.dataConnection;
        if (!connection) {
            this.subtitle = undefined;
            this.subtitleIconClass = undefined;
            this.subtitleHighlightClass = undefined;
            return;
        }

        this.subtitle = connection.name;
        this.subtitleIconClass = "database-icon";

        // `containsProductionData` is defined on `DatabaseConnection` subclasses,
        // not on the abstract `DataConnection` base.
        const containsProductionData =
            (connection as { containsProductionData?: boolean }).containsProductionData === true;
        this.subtitleHighlightClass = containsProductionData ? "is-production" : undefined;
    }

    private updateTooltip(): void {
        const path = this.environment.script.path;
        if (path) {
            this.tooltip = path;
        } else if (this.isDirty) {
            this.tooltip = "Unsaved";
        } else {
            this.tooltip = undefined;
        }
    }

    private updateStatusIndicator(): void {
        const env = this.environment;

        switch (env.status) {
            case "Running":
                this.statusIndicator = "running";
                break;
            case "Stopping":
                this.statusIndicator = "stopping";
                break;
            case "Error":
                this.statusIndicator = "error";
                break;
            case "Ready":
                this.statusIndicator = env.runDurationMilliseconds != null ? "success" : undefined;
                break;
            default:
                this.statusIndicator = undefined;
                break;
        }
    }

    private async textChanged() {
        const code = this.textDocument.text;
        this.script.code = code;

        await this.scriptService.updateCode(this.environment.script.id, code, false);
    }
}
