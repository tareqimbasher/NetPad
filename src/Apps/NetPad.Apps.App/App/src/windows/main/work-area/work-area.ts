import {IContainer, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {
    IAppService,
    IEventBus,
    IScriptService,
    ISession,
    ScriptEnvironment
} from "@application";
import {ViewModelBase} from "@application/view-model-base";
import {ViewerHost} from "./viewers/viewer-host";
import {ViewableObject} from "./viewers/viewable-object";
import {ViewableScriptDocument} from "./viewers/script-viewer/viewable-script-document";
import {Workbench} from "../workbench";
import {RunScriptCommand} from "@application/scripts/run-script-command";

export class WorkArea extends ViewModelBase {
    constructor(
        private readonly workbench: Workbench,
        @ISession private readonly session: ISession,
        @IAppService private readonly appService: IAppService,
        @IScriptService private readonly scriptService: IScriptService,
        @IEventBus private readonly eventBus: IEventBus,
        @IContainer container: IContainer,
        @ILogger logger: ILogger,
    ) {
        super(logger);

        const viewHostFactory = container.getFactory(ViewerHost);
        this.workbench.workAreaService.viewerHosts.add(viewHostFactory.construct(container));
    }

    protected override async attaching() {
        super.attaching();

        const scriptDocuments = this.session.environments.map(env => this.createViewableScriptDocument(env));

        if (!this.workbench.workAreaService.viewerHosts.active) {
            await this.workbench.workAreaService.viewerHosts.activate(this.workbench.workAreaService.viewerHosts.items[0]);
        }

        this.workbench.workAreaService.viewerHosts.items[0].addViewables(...scriptDocuments);

        if (this.session.active) {
            this.activeEnvironmentChanged(this.session.active);
        }

        for (const viewerHost of this.workbench.workAreaService.viewerHosts.items.filter(x => !x.activeViewable && x.viewables.size > 0)) {
            const [viewable] = viewerHost.viewables;
            viewerHost.activate(viewable);
        }

        // Handle the "run active viewable" case (no scriptId in the command).
        // Specific scriptId cases are handled per-viewable in ViewableScriptDocument.
        // This active-case handler will move to WorkAreaService.initialize() in phase 5.
        this.addDisposable(
            this.eventBus.subscribe(RunScriptCommand, async msg => {
                if (msg.scriptId !== undefined) return;

                const activeViewable = this.workbench.workAreaService.viewerHosts.active?.activeViewable;
                if (activeViewable?.canRun()) {
                    await activeViewable.run();
                }
            })
        );
    }

    @watch<WorkArea>(vm => vm.session.environments.length)
    private environmentsChanged() {
        const environments = this.session.environments;

        // Additions
        for (const environment of environments) {
            if (this.workbench.workAreaService.viewerHosts.items.some(vh => vh.find(environment.script.id)))
                continue;

            this.workbench.workAreaService.viewerHosts.items[0].addViewables(this.createViewableScriptDocument(environment));
        }

        // Removals
        for (const viewerHost of this.workbench.workAreaService.viewerHosts.items) {
            const removed: ViewableObject[] = [];

            for (const viewable of viewerHost.viewables) {
                if (!(viewable instanceof ViewableScriptDocument))
                    continue;

                if (!environments.some(e => e.script.id === viewable.id))
                    removed.push(viewable);
            }

            viewerHost.removeViewables(...removed);

            for (const viewable of removed) {
                viewable.dispose();
            }
        }
    }

    @watch<WorkArea>(vm => vm.session.active)
    private activeEnvironmentChanged(newActive: ScriptEnvironment | null | undefined) {
        let documentTitle: string | undefined;

        try {
            if (!newActive) {
                return;
            }

            const result = this.workbench.workAreaService.viewerHosts.findViewable(newActive.script.id);
            if (result) {
                this.workbench.workAreaService.viewerHosts.activateViewable(result.viewable);
                documentTitle = result.viewable.name;
            }
        } finally {
            document.title = documentTitle ? `${documentTitle} - NetPad` : "NetPad";
        }
    }

    private createViewableScriptDocument(environment: ScriptEnvironment): ViewableScriptDocument {
        return new ViewableScriptDocument(
            environment,
            this.scriptService,
            this.session,
            this.appService,
            this.workbench.workAreaService,
            this.eventBus
        );
    }
}
