import {bindable, ILogger} from "aurelia";
import {
    DataConnection,
    DataConnectionStore,
    IAppService,
    IEventBus,
    IScriptService,
    Script,
    ScriptEnvironment,
    ScriptKind
} from "@domain";
import {IShortcutManager, ViewModelBase} from "@application";
import {ViewableAppScriptDocument, ViewableTextDocument} from "./viewable-text-document";

export class Toolbar extends ViewModelBase {
    @bindable viewable: ViewableTextDocument;
    public dotNetSdkVersion = "";
    private readonly baseLogger: ILogger;

    constructor(@IScriptService private readonly scriptService: IScriptService,
                @IAppService private readonly appService: IAppService,
                @IShortcutManager private readonly shortcutManager: IShortcutManager,
                @IEventBus private readonly eventBus: IEventBus,
                private readonly dataConnectionStore: DataConnectionStore,
                @ILogger logger: ILogger) {
        super(logger);
        this.baseLogger = this.logger;
    }

    public get environment(): ScriptEnvironment | null | undefined {
        return this.viewable instanceof ViewableAppScriptDocument
            ? this.viewable.environment
            : null;
    }

    public get script(): Script | null | undefined {
        return this.environment?.script;
    }

    public get kind(): ScriptKind | null | undefined {
        return this.script?.config.kind;
    }

    public set kind(value) {
        this.logger.debug("Setting script kind");

        if (!this.script || !value) return;

        this.scriptService.setScriptKind(this.script.id, value)
            .catch(err => {
                this.logger.error("Failed to set script kind", err);
            });
    }

    public get dataConnection(): DataConnection | undefined {
        const connection = this.script?.dataConnection;

        if (!connection)
            return undefined;

        // We want to return the connection object from the connection store, not the connection
        // defined in the script.dataConnection property because they both reference 2 different
        // object instances, even though they are "the same connection"
        return this.dataConnectionStore.connections.find(c => c.id == connection.id);
    }

    public set dataConnection(value: DataConnection | undefined) {
        this.logger.debug("Setting data connection");

        if (!this.script) return;

        this.scriptService.setDataConnection(this.script.id, value?.id)
            .catch(err => {
                this.logger.error("Failed to set script data connection", err);
            });
    }

    public attached() {
        this.appService.checkDependencies().then(result => {
            if (!result?.dotNetSdkVersions.length) {
                this.dotNetSdkVersion = "";
                return;
            }

            const latest = result.dotNetSdkVersions.sort((a, b) => -1 * a.localeCompare(b))[0];
            const firstChar = latest[0];

            this.dotNetSdkVersion = isNaN(Number(firstChar)) ? "" : firstChar;
        });
    }

    public async run() {
        if (this.viewable instanceof ViewableAppScriptDocument) {
            await this.viewable.run();
        }
    }

    public async save() {
        await this.viewable.save();
    }

    public getShortcutKeyCombo(shortcutName: string) {
        return this.shortcutManager.getShortcutByName(shortcutName)?.toString()
    }

    public async openProperties() {
        if (this.viewable instanceof ViewableAppScriptDocument) {
            await this.viewable.openProperties();
        }
    }

    private viewableChanged(newViewable: ViewableTextDocument) {
        this.logger = !this.script
            ? this.baseLogger
            : this.baseLogger.scopeTo(`[${this.script?.id}] ${this.environment?.script.name}`);
    }
}
