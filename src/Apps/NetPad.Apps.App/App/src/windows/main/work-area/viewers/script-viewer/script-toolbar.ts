import {bindable, ILogger} from "aurelia";
import {
    CommandIds,
    DataConnection,
    DataConnectionStore,
    DotNetFrameworkVersion,
    IAppService,
    ICommandRegistry,
    IEventBus,
    IKeybindingManager,
    IScriptService,
    NpValueSelect,
    OptimizationLevel,
    Script,
    ScriptEnvironment,
    ScriptKind,
    ValueSelectOption,
    ViewModelBase
} from "@application";
import {ViewableScriptDocument} from "./viewable-script-document";

const kindOptions: ValueSelectOption[] = [
    {value: "Program", label: "C# Program"},
    {value: "SQL", label: "SQL"},
];

const optimizationOptions: ValueSelectOption[] = [
    {value: "Debug", label: "Debug", detail: "Optimize-"},
    {value: "Release", label: "Release", detail: "Optimize+"},
];

export class ScriptToolbar extends ViewModelBase {
    @bindable viewable: ViewableScriptDocument;
    public availableFrameworkVersions: DotNetFrameworkVersion[] = [];
    public readonly kindOptions = kindOptions;
    public readonly optimizationOptions = optimizationOptions;

    public sdkSelect?: NpValueSelect;
    public kindSelect?: NpValueSelect;
    public optimizeSelect?: NpValueSelect;
    public connectionSelect?: NpValueSelect;

    private readonly baseLogger: ILogger;

    constructor(@IScriptService private readonly scriptService: IScriptService,
                @IAppService private readonly appService: IAppService,
                @ICommandRegistry private readonly commandRegistry: ICommandRegistry,
                @IKeybindingManager private readonly keybindingManager: IKeybindingManager,
                @IEventBus private readonly eventBus: IEventBus,
                private readonly dataConnectionStore: DataConnectionStore,
                @ILogger logger: ILogger) {
        super(logger);
        this.baseLogger = this.logger;
    }

    public get environment(): ScriptEnvironment | null | undefined {
        return this.viewable?.environment;
    }

    public get isBusy(): boolean {
        const status = this.environment?.status;
        return status === "Running" || status === "Stopping";
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

    public get sdkOptions(): ValueSelectOption[] {
        const installed = this.availableFrameworkVersions;
        const options = installed.map(v => ({value: v, label: ScriptToolbar.sdkLabel(v)}));

        const targeted = this.targetFrameworkVersion;
        if (targeted && !installed.includes(targeted)) {
            options.unshift({value: targeted, label: `${ScriptToolbar.sdkLabel(targeted)} (not installed)`});
        }

        return options;
    }

    public get dataConnectionOptions(): ValueSelectOption[] {
        return [
            {value: undefined, label: "None"},
            ...this.dataConnectionStore.connections.map(c => ({
                value: c,
                label: c.name,
                detail: c.type,
                icon: "database",
            })),
        ];
    }

    public get targetsProductionData(): boolean {
        return this.viewable?.hasProductionWarning === true;
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
        this.appService.getAvailableDotNetSdkVersions().then(result => this.availableFrameworkVersions = result);
    }

    public readonly commandIds = CommandIds;

    public async run() {
        await this.execute(CommandIds.runScript);
    }

    public async stop() {
        await this.execute(CommandIds.stopScript);
    }

    public async save() {
        await this.execute(CommandIds.saveScript);
    }

    public async openProperties() {
        await this.execute(CommandIds.openScriptProperties);
    }

    public tooltip(commandId: string): string {
        return this.keybindingManager.describe(commandId);
    }

    /** The bare key combination, for rendering inside a kbd cap. */
    public keys(commandId: string): string | undefined {
        return this.keybindingManager.keysFor(commandId);
    }

    private execute(commandId: string) {
        return this.commandRegistry.execute(commandId, this.script?.id);
    }

    private static sdkLabel(version: DotNetFrameworkVersion): string {
        return `.NET ${version.replace("DotNet", "")}`;
    }

    private viewableChanged() {
        this.logger = !this.script
            ? this.baseLogger
            : this.baseLogger.scopeTo(`[${this.script?.id}] ${this.environment?.script.name}`);
    }
}
