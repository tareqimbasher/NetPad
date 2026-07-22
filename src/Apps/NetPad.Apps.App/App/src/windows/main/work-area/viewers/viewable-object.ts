import {WithDisposables} from "@common";
import {IconName, ScriptStatusIndicator} from "@application";
import {ViewerHost} from "./viewer-host";
import {DragAndDropBase} from "@application/dnd/drag-and-drop-base";

export type ViewableStatusIndicator = ScriptStatusIndicator;

export abstract class ViewableObject extends WithDisposables {
    public kindBadge?: string;
    public tooltip?: string;
    public subtitle?: string;
    public subtitleIcon?: IconName;
    public hasProductionWarning?: boolean;
    public statusIndicator?: ViewableStatusIndicator;
    public path?: string;

    protected constructor(public readonly id: string) {
        super();
    }

    abstract get name(): string;

    abstract get isDirty(): boolean;

    public override toString() {
        return `${this.constructor.name} [${this.id}] ${this.name}`;
    }

    // Universal navigation operations.
    public abstract open(viewerHost: ViewerHost): Promise<void>;
    public abstract close(viewerHost: ViewerHost): Promise<void>;
    public abstract activate(viewerHost: ViewerHost): Promise<void>;

    // Capability + action method pairs. Subclasses override only what they support.

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
