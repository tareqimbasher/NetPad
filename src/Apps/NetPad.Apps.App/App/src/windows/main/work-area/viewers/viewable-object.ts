import {WithDisposables} from "@common";
import {ViewerHost} from "./viewer-host";
import {Script} from "@application";

export interface IViewableObjectCommands
{
    open: (viewerHost: ViewerHost) => Promise<void>;
    close: (viewerHost: ViewerHost) => Promise<void>;
    activate: (viewerHost: ViewerHost) => Promise<void>;
    rename: () => Promise<void>;
    duplicate: () => Promise<Script>;
    save: () => Promise<boolean>;
    openContainingFolder: () => Promise<void>;
}

export abstract class ViewableObject extends WithDisposables {
    protected constructor(
        public readonly id: string,
        protected readonly commands: IViewableObjectCommands
    ) {
        super();
    }

    abstract get name(): string;

    abstract get isDirty(): boolean;

    public override toString() {
        return `${(this as Record<string, unknown>).constructor.name} [${this.id}] ${this.name}`;
    }

    public open(viewerHost: ViewerHost): Promise<void> {
        return this.commands.open(viewerHost);
    }

    public close(viewerHost: ViewerHost): Promise<void> {
        return this.commands.close(viewerHost);
    }

    public activate(viewerHost: ViewerHost): Promise<void> {
        return this.commands.activate(viewerHost);
    }

    public rename(): Promise<void> {
        return this.commands.rename();
    }

    public duplicate(): Promise<Script> {
        return this.commands.duplicate();
    }

    public save(): Promise<boolean> {
        return this.commands.save();
    }

    public openContainingFolder(): Promise<void> {
        return this.commands.openContainingFolder();
    }
}
