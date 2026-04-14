import {WithDisposables} from "@common";
import {observable} from "@aurelia/runtime";
import {ViewerHost} from "./viewer-host";
import {DragAndDropBase} from "@application/dnd/drag-and-drop-base";

export type ViewableStatusIndicator = "running" | "stopping" | "success" | "error";

export abstract class ViewableObject extends WithDisposables {
    // Generic display surface consumed by the tab bar and any other chrome that
    // needs to render a viewable without knowing its concrete type. All dynamic
    // values must be updated via assignments (not computed in getters) so Aurelia
    // bindings react to changes — getter call graphs are NOT tracked by the
    // binding observer system.
    @observable public iconImageSrc?: string;
    @observable public iconClass?: string;
    @observable public subtitle?: string;
    @observable public subtitleIconClass?: string;
    @observable public subtitleHighlightClass?: string;
    @observable public tooltip?: string;
    @observable public statusIndicator?: ViewableStatusIndicator;
    @observable public path?: string;

    protected constructor(public readonly id: string) {
        super();
    }

    abstract get name(): string;

    abstract get isDirty(): boolean;

    public override toString() {
        return `${(this as Record<string, unknown>).constructor.name} [${this.id}] ${this.name}`;
    }

    // Universal navigation operations — every viewable must implement these.
    public abstract open(viewerHost: ViewerHost): Promise<void>;
    public abstract close(viewerHost: ViewerHost): Promise<void>;
    public abstract activate(viewerHost: ViewerHost): Promise<void>;

    // Capability + action method pairs. Subclasses override only what they support.
    // The tab bar and context menu use these to conditionally show menu items and
    // dispatch commands without knowing the concrete viewable type.

    public canSave(): boolean {
        return false;
    }

    public save(): Promise<boolean> {
        return Promise.resolve(false);
    }

    public canRename(): boolean {
        return false;
    }

    public rename(): Promise<void> {
        return Promise.resolve();
    }

    public canDuplicate(): boolean {
        return false;
    }

    public duplicate(): Promise<void> {
        return Promise.resolve();
    }

    public canOpenContainingFolder(): boolean {
        return false;
    }

    public openContainingFolder(): Promise<void> {
        return Promise.resolve();
    }

    public canRun(): boolean {
        return false;
    }

    public run(): Promise<void> {
        return Promise.resolve();
    }

    public canStop(): boolean {
        return false;
    }

    public stop(): Promise<void> {
        return Promise.resolve();
    }

    public canOpenProperties(): boolean {
        return false;
    }

    public openProperties(): Promise<void> {
        return Promise.resolve();
    }

    public canHandleDrop(_dnd: DragAndDropBase | null | undefined): boolean {
        return false;
    }

    public handleDrop(_dnd: DragAndDropBase): Promise<void> {
        return Promise.resolve();
    }
}
