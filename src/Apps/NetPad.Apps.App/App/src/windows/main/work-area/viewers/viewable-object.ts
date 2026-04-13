import {WithDisposables} from "@common";
import {observable} from "@aurelia/runtime";
import {ViewerHost} from "./viewer-host";
import {Script} from "@application";

export type ViewableStatusIndicator = "running" | "stopping" | "success" | "error";

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
