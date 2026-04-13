import {Constructable, IContainer, ILogger} from "aurelia";
import {ViewableObject} from "./viewable-object";
import {Viewer} from "./viewer";
import {IViewerRegistry} from "./viewer-registry";
import {Util} from "@common";

export class ViewerHost {
    public readonly id: string;
    public order = 0;

    private readonly logger: ILogger;
    private _viewables = new Set<ViewableObject>();
    private _activeViewable?: ViewableObject;
    private _viewers = new Map<Constructable<Viewer>, Viewer>();
    private _activeViewer: Viewer;

    constructor(
        @IContainer private readonly container: IContainer,
        @IViewerRegistry private readonly viewerRegistry: IViewerRegistry,
        @ILogger logger: ILogger
    ) {
        this.id = Util.newGuid();
        this.logger = logger.scopeTo(nameof(ViewerHost));
    }

    public get viewables(): ReadonlySet<ViewableObject> {
        return this._viewables;
    }

    public get activeViewable(): ViewableObject | undefined {
        return this._activeViewable;
    }

    public get viewers(): ReadonlyArray<Viewer> {
        return Array.from(this._viewers.values());
    }

    public get activeViewer(): Viewer {
        return this._activeViewer;
    }

    public find(id: string): ViewableObject | undefined {
        return Array.from(this.viewables).find(v => v.id === id);
    }

    public addViewables(...viewables: ViewableObject[]) {
        if (viewables.length === 0) return;

        for (const viewable of viewables) {
            if (!this._viewables.has(viewable)) {
                this.logger.debug(`Adding viewable: ${viewable.toString()}`)
                this._viewables.add(viewable);
            }
        }
    }

    public removeViewables(...viewables: ViewableObject[]) {
        if (viewables.length === 0) return;

        for (const viewable of viewables) {
            this.logger.debug(`Removing viewable ${viewable.toString()}`)

            this.logger.debug(`Looking for viewer ${viewable.toString()}`)
            const viewer = this.findViewerFor(viewable);
            if (viewer) {
                this.logger.debug(`Viewer found ${viewable.toString()}, closing with viewer`);
                viewer.close(viewable);
            } else {
                this.logger.debug(`No viewer found`);
            }

            this._viewables.delete(viewable);
        }

        this.removeUnneededViewers();
    }

    public activate(viewable: ViewableObject) {
        if (!this.viewables.has(viewable))
            throw new Error("Viewable is not opened yet and so cannot be activated.");

        const viewer = this.getViewer(viewable);

        if (!viewer) {
            this.logger.error(`Viewer not registered for viewable: ${viewable.toString()}`);
            return;
        }

        this.logger.debug(`Activating viewable: ${viewable.toString()}`);
        viewer.open(viewable);
        this._activeViewable = viewable;
        this._activeViewer = viewer;
    }

    private getViewer(viewable: ViewableObject): Viewer | undefined {
        const viewerClass = this.viewerRegistry.resolve(viewable);
        if (!viewerClass) {
            return undefined;
        }

        let viewer = this._viewers.get(viewerClass);
        if (viewer) {
            return viewer;
        }

        // Create a new viewer instance scoped to this host. Each ViewerHost has its own
        // viewer instances so that split panes can display different viewables simultaneously.
        const factory = this.container.getFactory(viewerClass);
        viewer = factory.construct(this.container);
        viewer.setHost(this);
        this._viewers.set(viewerClass, viewer);

        return viewer;
    }

    private findViewerFor(viewable: ViewableObject): Viewer | undefined {
        for (const viewer of this._viewers.values()) {
            if (viewer.canOpen(viewable)) {
                return viewer;
            }
        }
        return undefined;
    }

    private removeUnneededViewers() {
        const viewables = Array.from(this.viewables);
        const viewerClassesToDispose: Constructable<Viewer>[] = [];

        for (const [viewerClass, viewer] of this._viewers) {
            if (viewables.some(v => viewer.canOpen(v))) {
                continue;
            }

            viewerClassesToDispose.push(viewerClass);
        }

        for (const viewerClass of viewerClassesToDispose) {
            const viewer = this._viewers.get(viewerClass);
            this.logger.debug(`Removing unneeded viewer: ${viewerClass.name}`);
            viewer?.dispose();
            this._viewers.delete(viewerClass);
        }
    }
}
