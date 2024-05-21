import {ILogger, resolve} from "aurelia";
import {PaneHost, PaneHostOrientation, PaneHostViewMode, Shortcut} from "@application";

export abstract class Pane {
    protected _name: string;
    protected _host?: PaneHost;
    protected _shortcut?: Shortcut;
    protected logger: ILogger;

    protected constructor(name: string, public readonly icon?: string, public readonly showNameInHeader: boolean = true) {
        this._name = name;
        this.logger = resolve(ILogger).scopeTo((this as Record<string, unknown>).constructor.name)
    }

    public get name(): string {
        return this._name;
    }

    public get host(): PaneHost | null | undefined {
        return this._host;
    }

    public get shortcut(): Shortcut | null | undefined {
        return this._shortcut;
    }

    public get isOpen(): boolean {
        return this.host?.viewMode === PaneHostViewMode.Expanded
            && this.host?.active === this;
    }

    public get orientation(): PaneHostOrientation | undefined {
        return this.host?.orientation;
    }

    public get isWindow(): boolean {
        return this.host?.orientation === PaneHostOrientation.FloatingWindow;
    }

    public setHost(paneHost: PaneHost) {
        this._host = paneHost;
    }

    public activate() {
        this.host?.expand(this);
    }

    public hide() {
        this.host?.collapse(this);
    }

    public hasShortcut(shortcut?: Shortcut) {
        this._shortcut = shortcut;
    }
}
