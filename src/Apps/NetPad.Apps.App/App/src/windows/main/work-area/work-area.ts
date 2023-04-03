import {IContainer, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {IAppService, IEventBus, IScriptService, ISession, RunOptionsDto, ScriptEnvironment} from "@domain";
import {ViewModelBase} from "@application/view-model-base";
import {ViewerHost} from "./viewers/viewer-host";
import {ViewableObject} from "./viewers/viewable-object";
import {ViewerHostCollection} from "./viewers/viewer-host-collection";
import {WorkAreaAppearance} from "./work-area-appearance";
import {
    IViewableAppScriptDocumentCommands,
    ViewableAppScriptDocument
} from "./viewers/text-document-viewer/viewable-text-document";
import {RunScriptEvent} from "@application";

export class WorkArea extends ViewModelBase {
    public viewerHosts = new ViewerHostCollection();

    constructor(
        @ISession private readonly session: ISession,
        @IAppService private readonly appService: IAppService,
        @IScriptService private readonly scriptService: IScriptService,
        @IEventBus private readonly eventBus: IEventBus,
        @IContainer container: IContainer,
        @ILogger logger: ILogger,
        private readonly appearance: WorkAreaAppearance,
    ) {
        super(logger);

        const viewHostFactory = container.getFactory(ViewerHost);
        this.viewerHosts.push(viewHostFactory.construct(container));
        //this.viewerHosts.push(viewHostFactory.construct(container));

        this.appearance.load();
        this.addDisposable(this.appearance);
    }

    public override async attaching() {
        super.attaching();

        const scriptDocuments = this.session.environments.map(env => this.createViewableAppScriptDocument(env));

        if (!this.viewerHosts.active) {
            await this.viewerHosts.activate(this.viewerHosts[0]);
        }

        this.viewerHosts[0].addViewables(...scriptDocuments);

        const activeScript = this.session.active && scriptDocuments.find(s => s.environment === this.session.active);
        if (activeScript) {
            this.viewerHosts.activateViewable(activeScript);
        }

        for (const viewerHost of this.viewerHosts.filter(x => !x.activeViewable && x.viewables.size > 0)) {
            const [viewable] = viewerHost.viewables;
            viewerHost.activate(viewable);
        }

        this.addDisposable(
            this.eventBus.subscribe(RunScriptEvent, async msg => {
                const scriptId = msg.scriptId ?? this.viewerHosts.active?.activeViewable?.id;

                if (!scriptId) return;

                const result = this.viewerHosts.findViewable(scriptId);
                if (result?.viewable instanceof ViewableAppScriptDocument)
                    await result.viewable.run();
            })
        );
    }

    @watch<WorkArea>(vm => vm.session.environments.length)
    private environmentsChanged() {
        const environments = this.session.environments;

        // Additions
        for (const environment of environments) {
            if (this.viewerHosts.some(vh => vh.find(environment.script.id)))
                continue;

            this.viewerHosts[0].addViewables(this.createViewableAppScriptDocument(environment));
        }

        // Removals
        for (const viewerHost of this.viewerHosts) {
            const removed: ViewableObject[] = [];

            for (const viewable of viewerHost.viewables) {
                if (!(viewable instanceof ViewableAppScriptDocument))
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
    private activeEnvironmentChanged(newActive: ScriptEnvironment) {
        const result = this.viewerHosts.findViewable(newActive.script.id);
        if (result) {
            this.viewerHosts.activateViewable(result.viewable);
        }
    }

    private createViewableAppScriptDocument(environment: ScriptEnvironment): ViewableAppScriptDocument {
        // TypeScript compiler incorrectly flags this that it should be 'const'
        // eslint-disable-next-line prefer-const
        let viewable: ViewableAppScriptDocument;

        const commands: IViewableAppScriptDocumentCommands = {
            open: async (viewerHost) => {
                viewerHost.addViewables(viewable)
            },
            close: async (viewerHost) => {
                const openInOtherViewerHosts = this.viewerHosts.find(x => x !== viewerHost && x.viewables.has(viewable));

                if (openInOtherViewerHosts) {
                    viewerHost.removeViewables(viewable);
                    // TODO What tab should be activated?
                } else {
                    await this.session.close(environment.script.id);
                }
            },
            activate: async (viewerHost) => await this.session.activate(environment.script.id),
            save: async () => await this.scriptService.save(environment.script.id),
            openContainingFolder: async () => environment.script.path
                ? await this.appService.openFolderContainingScript(environment.script.path)
                : Promise.reject("Script has not been saved yet"),
            updateCode: async (newCode: string) => {
                await this.scriptService.updateCode(viewable.script.id, newCode);
            },
            run: async () => {
                const document = viewable.textDocument;
                const runOptions = new RunOptionsDto();

                if (document.selection && !document.selection.isEmpty()) {
                    runOptions.specificCodeToRun = document.textModel.getValueInRange(document.selection);
                }

                await this.scriptService.run(environment.script.id, runOptions);
            },
            stop: async () => await this.scriptService.stop(environment.script.id),
            openProperties: async () => await this.scriptService.openConfigWindow(environment.script.id, null)
        };

        viewable = new ViewableAppScriptDocument(
            environment,
            commands
        );

        return viewable;
    }
}

