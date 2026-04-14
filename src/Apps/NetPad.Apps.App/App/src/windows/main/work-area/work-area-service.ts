import {DI, IContainer} from "aurelia";
import {WithDisposables} from "@common";
import {
    IAppService,
    IEventBus,
    IScriptService,
    ISession,
    ScriptEnvironment,
} from "@application";
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
     * Creates the initial viewer host and subscribes to workbench-level commands.
     * Idempotent — safe to call multiple times.
     */
    initialize(): void;

    findViewable(id: string): { viewable: ViewableObject, host: ViewerHost } | undefined;

    /**
     * Adds the viewable to a host (the supplied one, otherwise the active host,
     * otherwise the first host).
     */
    open(viewable: ViewableObject, targetHost?: ViewerHost): Promise<void>;

    /**
     * Closes the viewable in whichever host it currently lives.
     */
    close(viewableId: string): Promise<void>;

    /**
     * Activates the viewable in whichever host it currently lives.
     */
    activate(viewableId: string): void;

    /**
     * Factory for script viewables. Script-specific for now; when additional
     * viewable types are added, extract a separate IViewableFactory.
     */
    createScriptViewable(environment: ScriptEnvironment): ViewableScriptDocument;
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
    ) {
        super();
        this.appearance.load();
        this.addDisposable(this.appearance);
    }

    public get activeViewable(): ViewableObject | undefined {
        return this.viewerHosts.active?.activeViewable;
    }

    public initialize(): void {
        if (this._initialized) return;
        this._initialized = true;

        // Create the initial viewer host if none exist.
        if (this.viewerHosts.items.length === 0) {
            const viewHostFactory = this.container.getFactory(ViewerHost);
            this.viewerHosts.add(viewHostFactory.construct(this.container));
        }

        // Handle the "run active viewable" case of RunScriptCommand.
        // Per-viewable handlers inside ViewableScriptDocument cover the
        // case where a specific scriptId is provided.
        this.addDisposable(
            this.eventBus.subscribe(RunScriptCommand, async msg => {
                if (msg.scriptId !== undefined) return;

                const active = this.activeViewable;
                if (active?.canRun()) {
                    await active.run();
                }
            })
        );
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

    public async close(viewableId: string): Promise<void> {
        const result = this.findViewable(viewableId);
        if (!result) return;
        await result.viewable.close(result.host);
    }

    public activate(viewableId: string): void {
        const result = this.findViewable(viewableId);
        if (result) {
            result.host.activate(result.viewable);
        }
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
}
