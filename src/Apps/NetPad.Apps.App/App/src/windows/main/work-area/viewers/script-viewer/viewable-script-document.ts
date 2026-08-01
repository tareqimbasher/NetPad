import {
    EnvironmentPropertyChangedEvent,
    RunOptions,
    ScriptCodeUpdatedEvent,
    ScriptConfigPropertyChangedEvent,
    ScriptEnvironment,
    ScriptKind,
    ScriptPropertyChangedEvent,
} from "@application/api";
import {IAppService} from "@application/app/iapp-service";
import {IEventBus} from "@application/events/ievent-bus";
import {IScriptService} from "@application/scripts/iscript-service";
import {resolveScriptStatusIndicator} from "@application/scripts/script-status-indicator";
import {ISession} from "@application/sessions/isession";
import {ViewableTextDocument} from "../viewable-text-document";
import {ViewerHost} from "../viewer-host";
import {IWorkAreaService} from "../../work-area-service";
import {TextLanguage} from "@application/editor/text-language";
import {DndType} from "@application/dnd/dnd-type";
import {DragAndDropBase} from "@application/dnd/drag-and-drop-base";
import {DataConnectionDnd} from "@application/dnd/data-connection-dnd";
import {scriptKindBadge} from "@application/scripts/script-kind-badge";

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
            this.textDocument.onChange(async (setter) => {
                if (setter === "server") {
                    return;
                }
                await this.textChanged();
            })
        );

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
                    this.kindBadge = scriptKindBadge(ev.newValue as ScriptKind);
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
    }

    private static getLanguageFromScriptKind(kind: ScriptKind): TextLanguage {
        if (kind == "Program") return "csharp";
        else if (kind === "SQL") return "sql";
        else throw new Error("Unhandled script kind: " + kind);
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
        this.kindBadge = scriptKindBadge(this.environment.script.config.kind);
        this.path = this.environment.script.path;
        this.updateSubtitle();
        this.updateTooltip();
        this.updateStatusIndicator();
    }

    private updateSubtitle(): void {
        const connection = this.environment.script.dataConnection;
        if (!connection) {
            this.subtitle = undefined;
            this.subtitleIcon = undefined;
            this.hasProductionWarning = undefined;
            return;
        }

        this.subtitleIcon = "database";

        // `containsProductionData` is defined on `DatabaseConnection` subclasses,
        // not on the abstract `DataConnection` base.
        const containsProductionData =
            (connection as { containsProductionData?: boolean }).containsProductionData;
        this.subtitle = containsProductionData ? `${connection.name} (Production)` : connection.name;
        this.hasProductionWarning = containsProductionData;
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
        this.statusIndicator = resolveScriptStatusIndicator(this.environment);
    }

    private async textChanged() {
        const code = this.textDocument.text;
        this.script.code = code;

        await this.scriptService.updateCode(this.environment.script.id, code, false);
    }
}
