import {bindable, ILogger} from "aurelia";
import {
    DataConnection,
    DataConnectionStore,
    DotNetFrameworkVersion,
    IAppService,
    IEventBus,
    IScriptService,
    IShortcutManager,
    OptimizationLevel,
    Script,
    ScriptEnvironment,
    ScriptKind,
    ViewModelBase
} from "@application";
import {ViewableAppScriptDocument, ViewableTextDocument} from "./viewable-text-document";

export class Toolbar extends ViewModelBase {
    @bindable viewable: ViewableTextDocument;
    public availableFrameworkVersions: DotNetFrameworkVersion[] = [];
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
        if (!this.script || !value || this.script.config.kind === value) return;

        this.logger.debug("Setting script kind to:", value);

        this.scriptService.setScriptKind(this.script.id, value)
            .catch(err => {
                this.logger.error("Failed to set script kind", err);
            });
    }

    public get targetFrameworkVersion(): DotNetFrameworkVersion | null | undefined {
        return this.script?.config.targetFrameworkVersion;
    }

    public set targetFrameworkVersion(value) {
        if (!this.script || !value || this.script.config.targetFrameworkVersion === value) return;

        this.logger.debug("Setting targetFrameworkVersion to:", value);

        this.scriptService.setTargetFrameworkVersion(this.script.id, value)
            .catch(err => {
                this.logger.error("Failed to set script targetFrameworkVersion", err);
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
        if (!this.script || this.script.dataConnection?.id === value?.id) return;

        this.logger.debug("Setting data connection to:", value);

        this.scriptService.setDataConnection(this.script.id, value?.id)
            .catch(err => {
                this.logger.error("Failed to set script data connection", err);
            });
    }

    public get optimizationLevel(): OptimizationLevel | null | undefined {
        return this.script?.config.optimizationLevel;
    }

    public set optimizationLevel(value) {
        if (!this.script || !value || this.script.config.optimizationLevel === value) return;

        this.logger.debug("Setting optimizationLevel to:", value);

        this.scriptService.setOptimizationLevel(this.script.id, value)
            .catch(err => {
                this.logger.error("Failed to set script optimizationLevel", err);
            });
    }

    public attached() {
        this.appService.checkDependencies().then(result => {
            if (!result?.dotNetSdkVersions.length) {
                this.availableFrameworkVersions = [];
                return;
            }

            const frameworks = new Set<DotNetFrameworkVersion>();

            for (const sdkVersion of result.supportedDotNetSdkVersionsInstalled) {
                const major = sdkVersion.major;

                if (!isNaN(major) && major >= 2) {
                    frameworks.add(`DotNet${major}` as DotNetFrameworkVersion);
                }
            }

            this.availableFrameworkVersions = [...frameworks]
                .sort((a, b) => a.localeCompare(b));
        });
    }

    public async run() {
        if (this.viewable instanceof ViewableAppScriptDocument) {
            await this.viewable.run();
        }
    }

    public async stop() {
        if (this.viewable instanceof ViewableAppScriptDocument) {
            await this.viewable.stop();
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
