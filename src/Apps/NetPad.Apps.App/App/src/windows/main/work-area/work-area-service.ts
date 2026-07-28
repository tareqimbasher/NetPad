import {DI, IContainer, ILogger} from "aurelia";
import {WithDisposables} from "@common";
import {CreateScriptDto, EnvironmentsRemovedEvent, ScriptEnvironment} from "@application/api";
import {IAppService} from "@application/app/iapp-service";
import {IEventBus} from "@application/events/ievent-bus";
import {IScriptService} from "@application/scripts/iscript-service";
import {ISession} from "@application/sessions/isession";
import {RunScriptCommand} from "@application/scripts/run-script-command";
import {ViewerHostCollection} from "./viewers/viewer-host-collection";
import {ViewerHost} from "./viewers/viewer-host";
import {ViewableObject} from "./viewers/viewable-object";
import {ViewableScriptDocument} from "./viewers/script-viewer/viewable-script-document";
import {IWorkAreaAppearance} from "./work-area-appearance";

export const IWorkAreaService = DI.createInterface<IWorkAreaService>();

export interface IWorkAreaService {
    readonly viewerHosts: ViewerHostCollection;
    readonly appearance: IWorkAreaAppearance;
    readonly activeViewable: ViewableObject | undefined;

    /**
     * Creates the initial viewer host, subscribes to workbench-level commands, and
     * ensures at least one script is open. Idempotent — safe to call multiple times.
     */
    initialize(): Promise<void>;

    findViewable(id: string): { viewable: ViewableObject, host: ViewerHost } | undefined;

    /**
     * Adds the viewable to a host (the supplied one, otherwise the active host,
     * otherwise the first host).
     */
    open(viewable: ViewableObject, targetHost?: ViewerHost): Promise<void>;

    /**
     * Factory for script viewables. Script-specific for now; when additional
     * viewable types are added, extract a separate IViewableFactory.
     */
    createScriptViewable(environment: ScriptEnvironment): ViewableScriptDocument;

    /**
     * Creates a viewable for the environment and opens it, returning whether that succeeded.
     * Failures are logged instead of thrown so that a single script which cannot be opened does
     * not prevent the rest of the work area from being set up.
     */
    openScriptViewable(environment: ScriptEnvironment): Promise<boolean>;
}

export class WorkAreaService extends WithDisposables implements IWorkAreaService {
    public readonly viewerHosts = new ViewerHostCollection();
    private _initialized = false;

    constructor(
        @IContainer private readonly container: IContainer,
        @IWorkAreaAppearance public readonly appearance: IWorkAreaAppearance,
        @IScriptService private readonly scriptService: IScriptService,
        @ISession private readonly session: ISession,
        @IAppService private readonly appService: IAppService,
        @IEventBus private readonly eventBus: IEventBus,
        @ILogger private readonly logger: ILogger,
    ) {
        super();
        this.appearance.load();
        this.addDisposable(this.appearance);
    }

    public get activeViewable(): ViewableObject | undefined {
        return this.viewerHosts.active?.activeViewable;
    }

    public async initialize(): Promise<void> {
        if (this._initialized) return;
        this._initialized = true;

        // Create the initial viewer host if none exist.
        if (this.viewerHosts.items.length === 0) {
            const viewHostFactory = this.container.getFactory(ViewerHost);
            this.viewerHosts.add(viewHostFactory.construct(this.container));
        }

        // Handle RunScriptCommand
        this.addDisposable(
            this.eventBus.subscribe(RunScriptCommand, async msg => {
                const target = msg.scriptId !== undefined
                    ? this.findViewable(msg.scriptId)?.viewable
                    : this.activeViewable;

                if (target?.canRun()) {
                    await target.run();
                }
            })
        );

        // Always keep at least one script open.
        this.addDisposable(
            this.eventBus.subscribeToServer(EnvironmentsRemovedEvent, () => {
                void this.ensureAtLeastOneScript();
            })
        );

        await this.ensureAtLeastOneScript();
    }

    private async ensureAtLeastOneScript(): Promise<void> {
        if (this.session.environments.length === 0) {
            await this.scriptService.create(new CreateScriptDto());
        }
    }

    public findViewable(id: string): { viewable: ViewableObject, host: ViewerHost } | undefined {
        return this.viewerHosts.findViewable(id);
    }

    public async open(viewable: ViewableObject, targetHost?: ViewerHost): Promise<void> {
        const host = targetHost ?? this.viewerHosts.active ?? this.viewerHosts.items[0];
        if (!host) {
            throw new Error("No viewer host available to open viewable");
        }
        await viewable.open(host);
    }

    public createScriptViewable(environment: ScriptEnvironment): ViewableScriptDocument {
        return new ViewableScriptDocument(
            environment,
            this.scriptService,
            this.session,
            this.appService,
            this,
            this.eventBus,
        );
    }

    public async openScriptViewable(environment: ScriptEnvironment): Promise<boolean> {
        try {
            const viewable = this.createScriptViewable(environment);
            await this.open(viewable);
            return true;
        } catch (ex) {
            this.logger.error(
                `Could not open script '${environment.script.name}' (${environment.script.id}) in the work area.`,
                ex,
            );
            return false;
        }
    }
}
