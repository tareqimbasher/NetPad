import {IContainer, ILogger} from "aurelia";
import {ViewableObject, ViewableObjectType} from "./viewable-object";
import {Viewer} from "./viewer";
import {IEventBus, IScriptService} from "@application";
import {Util} from "@common";
import {Workbench} from "../../workbench";

export class ViewerHost {
    public readonly id: string;
    public order = 0;

    private readonly logger: ILogger;
    private _viewables = new Set<ViewableObject>();
    private _activeViewable?: ViewableObject;
    private _viewers = new Map<ViewableObjectType, Viewer>();
    private _activeViewer: Viewer;

    constructor(
        @IContainer private readonly container: IContainer,
        @IEventBus private readonly eventBus: IEventBus,
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
            const viewer = this._viewers.get(viewable.type);
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
            this.logger.error(`Not implemented: Viewer not implemented for view objects of type: ${viewable.type}`)
            return;
        }

        this.logger.debug(`Activating viewable: ${viewable.toString()}`);
        viewer.open(viewable);
        this._activeViewable = viewable;
        this._activeViewer = viewer;
    }

    private getViewer(viewable: ViewableObject) {
        let viewer = this._viewers.get(viewable.type);

        if (!viewer) {
            if (viewable.type === ViewableObjectType.Text) {
                // Using import here, with its async nature, makes this function async which is counter intuitive
                // eslint-disable-next-line @typescript-eslint/no-var-requires
                const TextDocumentViewer = require("./text-document-viewer/text-document-viewer").TextDocumentViewer;

                viewer = new TextDocumentViewer(
                    this,
                    this.container.get(Workbench),
                    this.container.get(IScriptService),
                    this.eventBus,
                    this.logger);

                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                this._viewers.set(ViewableObjectType.Text, viewer!);
            } else {
                // Implement more viewers for different object types
                // Maybe also implement a generic "error cannot open this type of file" kind of viewer
            }
        }

        return viewer;
    }

    private removeUnneededViewers() {
        const viewables = Array.from(this.viewables);
        const viewerTypesToDispose: ViewableObjectType[] = [];

        for (const [type, viewer] of this._viewers) {
            if (viewables.some(v => viewer.canOpen(v))) {
                continue;
            }

            viewerTypesToDispose.push(type);
        }

        for (const viewableObjectType of viewerTypesToDispose) {
            this.logger.debug(`Removing unneeded viewer for type: ${viewableObjectType}`);
            this._viewers.delete(viewableObjectType);
        }
    }
}
