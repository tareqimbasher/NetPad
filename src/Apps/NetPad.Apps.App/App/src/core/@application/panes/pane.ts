import {PaneHost, Shortcut} from "@application";

export abstract class Pane {
    protected _name: string;
    protected _host?: PaneHost;
    protected _shortcut?: Shortcut;

    protected constructor(name: string, protected readonly icon?: string) {
        this._name = name;
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

    public setHost(paneHost: PaneHost) {
        this._host = paneHost;
    }

    public activateOrCollapse() {
        this.host.activateOrCollapse(this);
    }

    public hasShortcut(shortcut: Shortcut) {
        this._shortcut = shortcut;
    }
}
